﻿using System;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;

namespace HueLights
{
    public class HueLight
    {
        public ushort BulbId;
        public String BulbName;
        public ushort BulbIsOn;
        public String BulbType;
        public ushort BulbBri;
        public ushort BulbHue;
        public ushort BulbSat;
	    public ushort BulbCt;
        public ushort BulbOnline;
        public ushort Reachable;

        //^^^^^ Signals for SIMPL+ ^^^^^^^^

	    private bool _foundBulb;
	    private string _url;
	    private JObject _json;
	    private string _jsontext;
	    private bool _supportsColor;
	    private int _bulbListId;

        public event EventHandler BulbBriUpdate;

        public event EventHandler BulbHueUpdate;

        public event EventHandler BulbSatUpdate;

        public event EventHandler BulbOnlineUpdate;

	    public event EventHandler BulbUpdate;

        public event EventHandler BulbOnOffUpdate;

        public HueLight()
        {

        }

	    public void BulbInit()
	    {
		    BulbOnline = 0;
			_bulbListId = 0;
			if (HueBridge.Populated == true)
			{
				BulbOnline = 0;
				for (int i = 0; i < HueBridge.HueBulbs.Count; i++)
				{
					if (HueBridge.HueBulbs[i].Name == BulbName)
					{
						BulbId = Convert.ToUInt16(HueBridge.HueBulbs[i].Id);
						//CrestronConsole.PrintLine("BulbID: {0}", HueBridge.HueBulbs[i].Id);
						_foundBulb = true;
						_bulbListId = i;
						break;
					}
				}
				if (_foundBulb == true)
				{
					BulbName = (String)HueBridge.HueBulbs[_bulbListId].Name;
					BulbIsOn = (ushort)(HueBridge.HueBulbs[_bulbListId].On ? 1 : 0);
					BulbType = (String)HueBridge.HueBulbs[_bulbListId].Type;
					BulbBri = (ushort)HueBridge.HueBulbs[_bulbListId].Bri;
					Reachable = (ushort)(HueBridge.HueBulbs[_bulbListId].Reachable ? 1 : 0);
					if (HueBridge.HueBulbs[_bulbListId].Type.Contains("Color"))
					{
						BulbHue = (ushort)(HueBridge.HueBulbs[_bulbListId].Hue);
						BulbSat = (ushort)(HueBridge.HueBulbs[_bulbListId].Sat);
					}
					BulbOnline = 1;
					CrestronConsole.PrintLine("Get {0} is complete", BulbName);
					TriggerBulbOnlineUpdate();
				}
				else
				{
					CrestronConsole.PrintLine("Bulb not found: {0}", BulbName);
				}
			}
	    }

        public void GetBulb()
        {
			//CrestronConsole.PrintLine("listID: {0}", _bulbListId);
	        try
	        {
		        if (_foundBulb == true)
		        {
			        _url = string.Format("http://{0}/api/{1}/{2}/{3}", HueBridge.BridgeIp, HueBridge.BridgeApi, "lights", BulbId);
					//CrestronConsole.PrintLine("url: {0}", _url);
			        _jsontext = HttpConnect.Instance.Request(_url, null, Crestron.SimplSharp.Net.Http.RequestType.Get);
			        _json = JObject.Parse(_jsontext);
					HueBridge.HueBulbs[_bulbListId].Reachable = (bool)_json["state"]["reachable"];
					HueBridge.HueBulbs[_bulbListId].On = (bool)_json["state"]["on"];
					HueBridge.HueBulbs[_bulbListId].Bri = (ushort)_json["state"]["bri"];
					BulbBri = (ushort)(HueBridge.HueBulbs[_bulbListId].Bri);
					if (_json["state"].SelectToken("colorMode") != null)
					{
						_supportsColor = true;
					}
			        if (_supportsColor)
			        {
						if (_json["state"].SelectToken("hue") != null)
						{
							HueBridge.HueBulbs[_bulbListId].Hue = (uint)_json["state"]["hue"];
						}
						if (_json["state"].SelectToken("sat") != null)
						{
							HueBridge.HueBulbs[_bulbListId].Sat = (uint)_json["state"]["sat"];
						}
						if (_json["state"].SelectToken("ct") != null)
						{
							HueBridge.HueBulbs[_bulbListId].Ct = (uint)_json["state"]["ct"];
						}
						BulbHue = (ushort)(HueBridge.HueBulbs[_bulbListId].Hue);
						BulbSat = (ushort)(HueBridge.HueBulbs[_bulbListId].Sat);
						BulbCt = (ushort)(HueBridge.HueBulbs[_bulbListId].Ct);
			        }
					BulbIsOn = (ushort)(HueBridge.HueBulbs[_bulbListId].On ? 1 : 0);
		        }
		        else
		        {
			        CrestronConsole.PrintLine("Error getting bulb data: {0}", BulbName);
		        }
		        TriggerBulbUpdate();
	        }
	        catch (Exception e)
	        {
		        CrestronConsole.PrintLine("Exception is {0}", e);
	        }
        }

        public void LightsAction(string lvltype, string val, string effect)
        {
            try
            {
                if (HueBridge.Authorized == true && HueBridge.Populated == true)
                {
                    Payload payload = new Payload() { SetType = "lights", LvlType = lvltype, OnOff = val, Effect = effect };
                    string json = HueBridge.SetCmd(PayloadType.BulbOnOff, payload, BulbId);
					JArray JReturn = JArray.Parse(json);
                    string tokenreturn = "/lights/" + BulbId + "/state/on";
                    foreach (var Jobj in JReturn)
                    {
                        var myaction = Jobj["success"];
                        string whodidwhat = myaction.ToString();
                        if (whodidwhat.Contains(tokenreturn))
                        {
                            HueBridge.HueBulbs[BulbId - 1].On = (bool)myaction[tokenreturn];
                            BulbIsOn = (ushort)(HueBridge.HueBulbs[BulbId - 1].On ? 1 : 0);
                            TriggerBulbOnOffUpdate();
                        }
                    }
                }
                else
                {
                    CrestronConsole.PrintLine("Error with Bulb Action {0}", BulbName);
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Exception is {0}", e);
            }
        }

        public void LightsVal(string lvltype, ushort val)
        {
            try
            {
                if (HueBridge.Authorized == true)
                {
                    var payload = new Payload() { SetType = "lights", Lvl = val, LvlType = lvltype };
                    var json = HueBridge.SetCmd(PayloadType.Lvl, payload, BulbId);
                    if (json.Contains("success"))
                    {
                        var jData = JArray.Parse(json);
                        var nodeVal = "/" + payload.SetType + "/" + BulbId + "/"+ payload.CmdType + "/" + lvltype;
                        HueBridge.HueBulbs[BulbId - 1].Bri = (uint)jData[0]["success"][nodeVal];
                        switch (lvltype)
                        {
                            case "bri":
                                {
                                    BulbBri = (ushort)HueBridge.HueBulbs[BulbId - 1].Bri;
                                    TriggerBulbBriUpdate();
                                    break;
                                }
                            case "hue":
                                {
                                    BulbHue = (ushort)HueBridge.HueBulbs[BulbId - 1].Hue;
                                    TriggerBulbHueUpdate();
                                    break;
                                }
                            case "sat":
                                {
                                    BulbSat = (ushort)HueBridge.HueBulbs[BulbId - 1].Sat;
                                    TriggerBulbSatUpdate();
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    CrestronConsole.PrintLine("Bridge not authorized");
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Exception is {0}", e);
            }
        }

        public void TriggerBulbBriUpdate()
        {
            BulbBriUpdate(this, new EventArgs());
        }

        public void TriggerBulbHueUpdate()
        {
            BulbHueUpdate(this, new EventArgs());
        }

        public void TriggerBulbSatUpdate()
        {
            BulbSatUpdate(this, new EventArgs());
        }

        public void TriggerBulbOnOffUpdate()
        {
            BulbOnOffUpdate(this, new EventArgs());
        }

	    public void TriggerBulbUpdate()
	    {
		    BulbUpdate(this, new EventArgs());
	    }

        public void TriggerBulbOnlineUpdate()
        {
            BulbOnlineUpdate(this, new EventArgs());
        }
    }
}
