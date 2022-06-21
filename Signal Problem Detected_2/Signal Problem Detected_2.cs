using System;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;

public class Script
{
	public void Run(IEngine engine)
	{
		string setParam = "";
		int setVal = 0;
		UIResults uir = null;
		string btnValue = "";
		string sel_textbox = "";
		string swTo = "";

		ScriptDummy TriggerIRD = engine.GetDummy("IRD");
		string triggerElement = TriggerIRD.ElementName;

		string[] trigger = triggerElement.Split('_');

		string _issueString = "An issue was detected with the uplink in ";


		if (trigger[3] == "MAIN")
		{
			Element element = engine.FindElement("IRD_EC_" + trigger[2] + "_BACKUP");
			int LNB1 = Convert.ToInt32(element.GetParameter("LNB1 Supply"));
			if (LNB1 == 1)
			{
				Element Switch = engine.FindElement("Site Switch");
				if (trigger[2] == "SITE1")
				{
					setParam = "Switch Site 1";
					swTo = "SITEA";
				}
				else
				{
					setParam = "Switch Site 2";
					swTo = "SITEB";
				}
				Switch.SetParameter(setParam, 2);
			}
			else
			{
				Element Switch = engine.FindElement("Site Switch");

				if ((trigger[2] == "SITE1" && Convert.ToString(Switch.GetParameter(300)) == "1") || (trigger[2] == "SITE2" && Convert.ToString(Switch.GetParameter(300)) == "2"))
				{

					Switch.SetParameter("Video Selection", "6");
					string allowedGroups = ""; //"Skyline - Administrators;Administrator";
					bool ok = engine.FindInteractiveClient(trigger[2] + " Signal Reception Failure", 100, allowedGroups, AutomationScriptAttachOptions.None);
					if (!ok)
					{
						engine.GenerateInformation("No active users found");
					}
					else
					{
						engine.GenerateInformation("Attached! As " + engine.UserDisplayName);
						// Create the dialog box		

						if (trigger[2] == "SITE1")
						{
							swTo = "SITE2";
						}
						else
						{
							swTo = "SITE1";
						}

						do
						{
							UIBuilder uib = new UIBuilder();

							// Configure the dialog box
							uib.RequireResponse = true;
							uib.RowDefs = "a;a;a";
							uib.ColumnDefs = "90;200;200";

							// Add a 'StaticText' item to the dialog box
							UIBlockDefinition blockStaticText = new UIBlockDefinition();
							blockStaticText.Type = UIBlockType.StaticText;
							blockStaticText.Text = _issueString + trigger[2] + ".\r\n\r\nCheck live signal feed from satellite downlink.\r\n\r\nDo you want to switch the uplink to the alternate site in " + swTo + " ?";
							blockStaticText.Row = 0;
							blockStaticText.Column = 0;
							blockStaticText.ColumnSpan = 3;
							uib.AppendBlock(blockStaticText);

							// Add a 'StaticText' item to the dialog box
							UIBlockDefinition blockStaticText1 = new UIBlockDefinition();
							blockStaticText1.Type = UIBlockType.StaticText;
							blockStaticText1.Text = "\r\n";
							blockStaticText1.Row = 1;
							blockStaticText1.Column = 0;
							blockStaticText1.ColumnSpan = 3;
							uib.AppendBlock(blockStaticText1);

							// Add a button to the dialog box
							UIBlockDefinition blockButtonNo = new UIBlockDefinition();
							blockButtonNo.Type = UIBlockType.Button;
							blockButtonNo.Text = "NO";
							blockButtonNo.Height = 25;
							blockButtonNo.Width = 75;
							blockButtonNo.Row = 2;
							blockButtonNo.Column = 1;
							blockButtonNo.DestVar = "btnNo";
							uib.AppendBlock(blockButtonNo);

							// Add a button to the dialog box
							UIBlockDefinition blockButtonYes = new UIBlockDefinition();
							blockButtonYes.Type = UIBlockType.Button;
							blockButtonYes.Text = "YES";
							blockButtonYes.Height = 25;
							blockButtonYes.Width = 75;
							blockButtonYes.Row = 2;
							blockButtonYes.Column = 0;
							blockButtonYes.DestVar = "btnYes";
							uib.AppendBlock(blockButtonYes);

							// Display the dialog box
							uir = engine.ShowUI(uib);

							if (uir.WasButtonPressed("btnYes"))
							{
								btnValue = "Yes";


							}
							else if (uir.WasButtonPressed("btnNo"))
							{
								btnValue = "No";
							}

						}
						while (!uir.WasButtonPressed("btnNo") && !uir.WasButtonPressed("btnYes"));

						if (btnValue == "No")
						{
							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "210;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Please specify why the switch was not executed.\r\nThis message will be sent to all noc users via mail.\r\n";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								//add a textbox
								UIBlockDefinition blockTextBox = new UIBlockDefinition();
								blockTextBox.Type = UIBlockType.TextBox;
								blockTextBox.IsMultiline = true;
								blockTextBox.InitialValue = sel_textbox;
								blockTextBox.DestVar = "textBox1";
								blockTextBox.Row = 1;
								blockTextBox.Column = 0;
								blockTextBox.Width = 200;
								blockTextBox.Height = 60;
								uib.AppendBlock(blockTextBox);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 2;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));

							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "200;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Message successfully sent.";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 1;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));
						}
						else
						{
							//Element Switch = engine.FindElement("Site Switch A");
							if (trigger[2] == "SITE1")
							{
								setParam = "Switch Site Selection";
								swTo = "SITE2";
								setVal = 2;
							}
							else
							{
								setParam = "Switch Site Selection";
								swTo = "SITE1";
								setVal = 1;
							}
							Switch.SetParameter(setParam, setVal);



							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "200;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Successfully switched to " + swTo + ".";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 1;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));
							Switch.SetParameter("Video Selection", "4");
						}
					}
				}
			}
		}
		else
		{

			Element element = engine.FindElement("IRD_EC_" + trigger[2] + "_MAIN");

			int LNB1 = Convert.ToInt32(element.GetParameter("LNB1 Supply"));
			if (LNB1 == 1)
			{
				Element Switch = engine.FindElement("Site Switch");
				if (trigger[2] == "SITE1")
				{
					setParam = "Switch Site 1";
					swTo = "SITE2";
				}
				else
				{
					setParam = "Switch Site 2";
					swTo = "SITE1";
				}
				Switch.SetParameter(setParam, 1);
			}
			else
			{
				Element Switch = engine.FindElement("Site Switch");
				engine.GenerateInformation("step 2 " + Switch.ElementName);

				if ((trigger[2] == "SITE1" && Convert.ToString(Switch.GetParameter(300)) == "1") || (trigger[2] == "SITE2" && Convert.ToString(Switch.GetParameter(300)) == "2"))
				{

					Switch.SetParameter("Video Selection", "6");
					string allowedGroups = ""; //"Skyline - Administrators;Administrator";
					bool ok = engine.FindInteractiveClient(trigger[2] + " Signal Reception Failure", 100, allowedGroups, AutomationScriptAttachOptions.None);
					if (!ok)
					{
						engine.GenerateInformation("No active users found");
					}
					else
					{
						engine.GenerateInformation("Attached! As " + engine.UserDisplayName);
						//engine.ShowProgress("Helloe");
						//engine.ShowUI("piepeloe", true);
						// Create the dialog box		

						if (trigger[2] == "SITE1")
						{
							swTo = "SITE2";
						}
						else
						{
							swTo = "SITE1";
						}

						do
						{
							UIBuilder uib = new UIBuilder();

							// Configure the dialog box
							uib.RequireResponse = true;
							uib.RowDefs = "a;a;a";
							uib.ColumnDefs = "90;200;200";

							// Add a 'StaticText' item to the dialog box
							UIBlockDefinition blockStaticText = new UIBlockDefinition();
							blockStaticText.Type = UIBlockType.StaticText;
							blockStaticText.Text = _issueString + trigger[2] + ".\r\n\r\nCheck live signal feed from satellite downlink.\r\n\r\nDo you want to switch the uplink to the alternate site in " + swTo + " ?";
							blockStaticText.Row = 0;
							blockStaticText.Column = 0;
							blockStaticText.ColumnSpan = 3;
							uib.AppendBlock(blockStaticText);

							// Add a 'StaticText' item to the dialog box
							UIBlockDefinition blockStaticText1 = new UIBlockDefinition();
							blockStaticText1.Type = UIBlockType.StaticText;
							blockStaticText1.Text = "\r\n";
							blockStaticText1.Row = 1;
							blockStaticText1.Column = 0;
							blockStaticText1.ColumnSpan = 3;
							uib.AppendBlock(blockStaticText1);

							// Add a button to the dialog box
							UIBlockDefinition blockButtonNo = new UIBlockDefinition();
							blockButtonNo.Type = UIBlockType.Button;
							blockButtonNo.Text = "NO";
							blockButtonNo.Height = 25;
							blockButtonNo.Width = 75;
							blockButtonNo.Row = 2;
							blockButtonNo.Column = 1;
							blockButtonNo.DestVar = "btnNo";
							uib.AppendBlock(blockButtonNo);

							// Add a button to the dialog box
							UIBlockDefinition blockButtonYes = new UIBlockDefinition();
							blockButtonYes.Type = UIBlockType.Button;
							blockButtonYes.Text = "YES";
							blockButtonYes.Height = 25;
							blockButtonYes.Width = 75;
							blockButtonYes.Row = 2;
							blockButtonYes.Column = 0;
							blockButtonYes.DestVar = "btnYes";
							uib.AppendBlock(blockButtonYes);

							// Display the dialog box
							uir = engine.ShowUI(uib);

							if (uir.WasButtonPressed("btnYes"))
							{
								btnValue = "Yes";
							}
							else if (uir.WasButtonPressed("btnNo"))
							{
								btnValue = "No";
							}

						}
						while (!uir.WasButtonPressed("btnNo") && !uir.WasButtonPressed("btnYes"));

						if (btnValue == "No")
						{
							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "210;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Please specify why the switch was not executed.\r\nThis message will be sent to all noc users via mail.\r\n";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								//add a textbox
								UIBlockDefinition blockTextBox = new UIBlockDefinition();
								blockTextBox.Type = UIBlockType.TextBox;
								blockTextBox.IsMultiline = true;
								blockTextBox.InitialValue = sel_textbox;
								blockTextBox.DestVar = "textBox1";
								blockTextBox.Row = 1;
								blockTextBox.Column = 0;
								blockTextBox.Width = 200;
								blockTextBox.Height = 60;
								uib.AppendBlock(blockTextBox);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 2;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));

							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "200;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Message successfully sent.";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 1;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));
						}
						else
						{


							//	Element Switch = engine.FindElement("Site Switch A");
							if (trigger[2] == "SITE1")
							{
								setParam = "Switch Site Selection";
								setVal = 2;
								swTo = "SITE2";
							}
							else
							{
								setParam = "Switch Site Selection";
								setVal = 1;
								swTo = "SITE1";
							}
							Switch.SetParameter(setParam, setVal);

							try
							{
								SubScriptOptions subscript = engine.PrepareSubScript("SlackAlert");
								subscript.PerformChecks = false;
								subscript.Synchronous = true;
								subscript.StartScript();
							}
							catch (Exception e)
							{
								engine.GenerateInformation(e.ToString());
							}

							try
							{
								SubScriptOptions subscriptTeams = engine.PrepareSubScript("TeamsAlert");
								subscriptTeams.PerformChecks = false;
								subscriptTeams.Synchronous = true;
								subscriptTeams.StartScript();
							}
							catch (Exception e)
							{
								engine.GenerateInformation(e.ToString());
							}

							do
							{
								// Create the dialog box
								UIBuilder uib = new UIBuilder();

								// Configure the dialog box
								uib.RequireResponse = true;
								uib.RowDefs = "a;a;a";
								uib.ColumnDefs = "200;200;200";

								// Add a 'StaticText' item to the dialog box
								UIBlockDefinition blockStaticText = new UIBlockDefinition();
								blockStaticText.Type = UIBlockType.StaticText;
								blockStaticText.Text = "Successfully switched to " + swTo + ".";
								blockStaticText.Row = 0;
								blockStaticText.Column = 0;
								blockStaticText.ColumnSpan = 3;
								blockStaticText.IsMultiline = true;
								uib.AppendBlock(blockStaticText);

								// Add a button to the dialog box
								UIBlockDefinition blockButtonContinue = new UIBlockDefinition();
								blockButtonContinue.Type = UIBlockType.Button;
								blockButtonContinue.Text = "Continue";
								blockButtonContinue.Height = 25;
								blockButtonContinue.Width = 75;
								blockButtonContinue.Row = 1;
								blockButtonContinue.Column = 0;
								blockButtonContinue.DestVar = "btnContinue";
								uib.AppendBlock(blockButtonContinue);

								// Display the dialog box
								uir = engine.ShowUI(uib);

							}
							while (!uir.WasButtonPressed("btnContinue"));
							Switch.SetParameter("Video Selection", "4");
						}
					}
				}
			}
		}
	}
}