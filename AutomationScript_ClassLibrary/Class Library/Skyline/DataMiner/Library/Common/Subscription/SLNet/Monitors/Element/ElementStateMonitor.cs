﻿namespace Skyline.DataMiner.Library.Common.Subscription.Monitors
{
	using Skyline.DataMiner.Library.Common.Selectors;
	using Skyline.DataMiner.Library.Common.SLNetHelper;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	using System;
	using System.Collections.Concurrent;
	using System.Threading;

	internal class ElementStateMonitor : Monitor
	{
		private Action<ElementStateChange> onChange;

		internal ElementStateMonitor(ICommunication connection, string sourceId, Element selection, string handleId) : base(connection, sourceId)
		{
			Initialize(selection, handleId);
		}

		internal ElementStateMonitor(ICommunication connection, Element sourceElement, Element selection, string handleId) : base(connection, sourceElement)
		{
			Initialize(selection, handleId);
		}

		internal ElementStateMonitor(ICommunication connection, Element sourceElement, Element selection) : this(connection, sourceElement, selection, "-State")
		{
		}

		internal ElementStateMonitor(ICommunication connection, string sourceId, Element selection) : this(connection, sourceId, selection, "-State")
		{
		}

		internal SLNetWaitHandle ActionHandle { get; private set; }

		internal Element Selection { get; private set; }

		internal void Start(Action<ElementStateChange> actionOnChange)
		{
			int agentId = Selection.AgentId;
			int elementId = Selection.ElementId;
			this.onChange = actionOnChange;

			if (elementId == -1)
			{

				ActionHandle.Handler = CreateHandler(ActionHandle.SetId, agentId, elementId);
				ActionHandle.Subscriptions = new [] { new SubscriptionFilter(typeof(ElementStateEventMessage), SubscriptionFilterOptions.SkipInitialEvents) };

				TryAddElementCleanup();
			}
			else
			{
				if(SourceElement != null && SourceElement.AgentId == Selection.AgentId && SourceElement.ElementId == Selection.ElementId)
				{
					// Subscribing to own element state.
					ActionHandle.Handler = CreateHandlerWithCleanup(ActionHandle.SetId, agentId, elementId);
					ActionHandle.Subscriptions = new SubscriptionFilter[] { new SubscriptionFilterElement(typeof(ElementStateEventMessage), agentId, elementId) };
				}
				else
				{
					ActionHandle.Handler = CreateHandler(ActionHandle.SetId, agentId, elementId);
					ActionHandle.Subscriptions = new SubscriptionFilter[] { new SubscriptionFilterElement(typeof(ElementStateEventMessage), agentId, elementId) };

					TryAddDestinationElementCleanup(agentId, elementId);
					TryAddElementCleanup();
				}
			}
			
			SubscriptionManager.CreateSubscription(SourceIdentifier, Connection, ActionHandle, true);
		}

		internal void Stop(bool force = false)
		{
			if (ActionHandle != null)
			{
				SubscriptionManager.RemoveSubscription(SourceIdentifier, Connection, ActionHandle, force);
				SubscriptionManager.TryRemoveCleanupSubs(SourceIdentifier, Connection);
			}
		}

		private static Common.ElementState ParseSlnetElementState(ElementStateEventMessage elementStateMessage)
		{
			return (Common.ElementState)elementStateMessage.State;
		}


		private NewMessageEventHandler CreateHandler(string HandleGuid, int dmaId, int eleId)
		{
			string myGuid = HandleGuid;
			return (sender, e) =>
			{
				try
				{
					if (!e.FromSet(myGuid)) return;

					var elementStateMessage = e.Message as ElementStateEventMessage;
					if (elementStateMessage == null) return;

					System.Diagnostics.Debug.WriteLine("State Event " + elementStateMessage.DataMinerID + "/" + elementStateMessage.ElementID + ":" + elementStateMessage.State + ":" + elementStateMessage.Level + ": complete=" + elementStateMessage.IsElementStartupComplete);

					bool isOnDmsLevel = dmaId == -1 && eleId == -1;
					bool isMatchWithElement = elementStateMessage.DataMinerID == dmaId && elementStateMessage.ElementID == eleId;

					if (isOnDmsLevel || isMatchWithElement)
					{
						System.Diagnostics.Debug.WriteLine("Match found.");
						HandleMatchingEvent(sender, myGuid, elementStateMessage);
					}
				}
				catch (Exception ex)
				{
					var message = "Monitor Error: Exception during Handle of ElementState event (Class Library Side): " + myGuid + " -- " + e + " With exception: " + ex;
					System.Diagnostics.Debug.WriteLine(message);
					Logger.Log(message);
				}
			};
		}

		private NewMessageEventHandler CreateHandlerWithCleanup(string HandleGuid, int dmaId, int eleId)
		{
			string myGuid = HandleGuid;
			return (sender, e) =>
			{
				try
				{
					if (!e.FromSet(myGuid)) return;

					var elementStateMessage = e.Message as ElementStateEventMessage;
					if (elementStateMessage == null) return;

					System.Diagnostics.Debug.WriteLine("State Event " + elementStateMessage.DataMinerID + "/" + elementStateMessage.ElementID + ":" + elementStateMessage.State + ":" + elementStateMessage.Level + ": complete=" + elementStateMessage.IsElementStartupComplete);

					bool isMatchWithElement = elementStateMessage.DataMinerID == dmaId && elementStateMessage.ElementID == eleId;

					if (isMatchWithElement)
					{

						System.Diagnostics.Debug.WriteLine("Match found.");
						var changed = GetElementStateChange(sender, myGuid, elementStateMessage);

						// clear subscriptions if element is stopped or deleted
						string uniqueIdentifier = elementStateMessage.DataMinerID + "/" + elementStateMessage.ElementID;
						var senderConn = (Connection)sender;
						if (elementStateMessage.State == Net.Messages.ElementState.Deleted || elementStateMessage.State == Net.Messages.ElementState.Stopped)
						{
							System.Diagnostics.Debug.WriteLine("Deleted or Stopped: Need to clean subscriptions");
							ICommunication com = new ConnectionCommunication(senderConn);
							SubscriptionManager.RemoveSubscriptions(uniqueIdentifier, com);
						}

						if (changed != null)
						{
							try
							{
								System.Diagnostics.Debug.WriteLine("executing action...");
								onChange(changed);
							}
							catch (Exception delegateEx)
							{
								var message = "Monitor Error: Exception during Handle of ElementState event (check provided action): " + myGuid + "-- With exception: " + delegateEx;
								System.Diagnostics.Debug.WriteLine(message);
								Logger.Log(message);
							}
						}
					}
				}
				catch (Exception ex)
				{
					var message = "Monitor Error: Exception during Handle of ElementState event (Class Library Side): " + myGuid + " -- " + e + " With exception: " + ex;
					System.Diagnostics.Debug.WriteLine(message);
					Logger.Log(message);
				}
			};
		}

		private void HandleMatchingEvent(object sender, string myGuid, ElementStateEventMessage elementStateMessage)
		{
			if (onChange == null) return;

			var senderConn = (Connection)sender;
			ICommunication com = new ConnectionCommunication(senderConn);

			Common.ElementState nonSLNetState = ParseSlnetElementState(elementStateMessage);

			if (nonSLNetState == Common.ElementState.Active && !elementStateMessage.IsElementStartupComplete) return;

			System.Diagnostics.Debug.WriteLine("executing action...");

			var changed = new ElementStateChange(new Element(elementStateMessage.DataMinerID, elementStateMessage.ElementID), SourceIdentifier, new Dms(com), nonSLNetState);
			if (SubscriptionManager.ReplaceIfDifferentCachedData(SourceIdentifier, myGuid, "Result_" + elementStateMessage.DataMinerID + "/" + elementStateMessage.ElementID, changed))
			{
				System.Diagnostics.Debug.WriteLine("Trigger Action - Different Result:" + nonSLNetState);

				try
				{
					onChange(changed);
				}
				catch (Exception delegateEx)
				{
					var message = "Monitor Error: Exception during Handle of ElementState event (check provided action): " + myGuid + "-- With exception: " + delegateEx;
					System.Diagnostics.Debug.WriteLine(message);
					Logger.Log(message);
				}
			}
		}

		/// <summary>
		/// Returns the corresponding ElementStateChange instance if it should be handled; otherwise, <see langword="null"/>.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="myGuid">The set ID.</param>
		/// <param name="elementStateMessage">The incoming element state message.</param>
		/// <returns>The ElementStateChange instance if it should be handled; otherwise, <see langword="null"/>.</returns>
		private ElementStateChange GetElementStateChange(object sender, string myGuid, ElementStateEventMessage elementStateMessage)
		{
			if (onChange == null) return null;

			var senderConn = (Connection)sender;
			ICommunication com = new ConnectionCommunication(senderConn);

			Common.ElementState nonSLNetState = ParseSlnetElementState(elementStateMessage);

			if (nonSLNetState == Common.ElementState.Active && !elementStateMessage.IsElementStartupComplete) return null;

			var changed = new ElementStateChange(new Element(elementStateMessage.DataMinerID, elementStateMessage.ElementID), SourceIdentifier, new Dms(com), nonSLNetState);

			if(SubscriptionManager.ReplaceIfDifferentCachedData(SourceIdentifier, myGuid, "Result_" + elementStateMessage.DataMinerID + "/" + elementStateMessage.ElementID, changed))
			{
				return changed;
			}

			return null;
		}

		private void Initialize(Element selection, string handleId)
		{
			Selection = selection;

			ActionHandle = new SLNetWaitHandle
			{
				Flag = new AutoResetEvent(false),
				SetId = SourceIdentifier + "-" + Selection + handleId,
				Type = WaitHandleType.Normal,
				Destination = Selection.AgentId + "/" + Selection.ElementId,
				TriggeredQueue = new ConcurrentQueue<object>(),
				CachedData = new ConcurrentDictionary<string, object>()
			};
		}
	}
}