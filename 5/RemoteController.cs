using System;
using System.Collections.Generic;
using System.Text;

namespace Refactoring
{
    public class RemoteController
    {
        public const int DEFAULT_VOLUME = 20;
        public const int DEFAULT_BRIGHTNESS = 20;
        public const int DEFAULT_CONTRAST = 20;

        public const int STEP_FOR_BUTTON = 10;

        public readonly Dictionary<string, int?> CurrentSettings = new Dictionary<string, int?>();
        public bool IsOnline = false;
        private readonly OptionsShower _optionsShower;

        public RemoteController()
        {
            _optionsShower = new OptionsShower(this);
        }

        public string Call(string command)
        {
            string subCommand = null;
            if (command.StartsWith("Options change"))
            {
                subCommand = command.Substring(14).Trim();
                command = "Options change";
            }

            switch (command)
            {
                case "Tv On":
                    IsOnline = true;
                    break;
                case "Tv Off":
                    if (CurrentSettings.ContainsKey("volume"))
                    {
                        for (int i = 0; i < CurrentSettings["volume"] / STEP_FOR_BUTTON; i++)
                        {
                            OptionsSwitch("volume down");
                        }
                    }

                    IsOnline = false;
                    break;
                case "Volume Up":
                    OptionsSwitch("volume up");
                    break;
                case "Volume Down":
                    OptionsSwitch("volume down");
                    break;
                case "Options change":
                    OptionsSwitch(subCommand);
                    break;
                case "Options show":
                    return _optionsShower.OptionsShow();
            }

            return "";
        }

        private void OptionsSwitch(string command)
        {
            switch (command.ToLower())
            {
                case "brightness up":
                    
                    if (!CurrentSettings.ContainsKey("brightness"))
                    {
                        CurrentSettings.Add("brightness", DEFAULT_BRIGHTNESS + STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["brightness"] += STEP_FOR_BUTTON;
                    }

                    break;
                case "brightness down":
                    if (!CurrentSettings.ContainsKey("brightness"))
                    {
                        CurrentSettings.Add("brightness", DEFAULT_BRIGHTNESS - STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["brightness"] -= STEP_FOR_BUTTON;
                    }

                    break;
                case "contrast up":
                    if (!CurrentSettings.ContainsKey("contrast"))
                    {
                        CurrentSettings.Add("contrast", DEFAULT_CONTRAST + STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["contrast"] += STEP_FOR_BUTTON;
                    }

                    break;
                case "contrast down":
                    if (!CurrentSettings.ContainsKey("contrast"))
                    {
                        CurrentSettings.Add("contrast", DEFAULT_CONTRAST - STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["contrast"] -= STEP_FOR_BUTTON;
                    }

                    break;
                case "volume up":
                    if (!CurrentSettings.ContainsKey("volume"))
                    {
                        CurrentSettings.Add("volume", DEFAULT_CONTRAST + STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["volume"] += STEP_FOR_BUTTON;
                    }

                    break;
                case "volume down":
                    if (!CurrentSettings.ContainsKey("volume"))
                    {
                        CurrentSettings.Add("volume", DEFAULT_VOLUME - STEP_FOR_BUTTON);
                    }
                    else
                    {
                        CurrentSettings["volume"] -= STEP_FOR_BUTTON;
                    }

                    break;
            }
        }
    }

    public class OptionsShower
    {
        private readonly RemoteController _remoteController;

        public OptionsShower(RemoteController remoteController)
        {
            _remoteController = remoteController;
        }

        public string OptionsShow()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Options:");

            sb.AppendLine($"Volume {(_remoteController.CurrentSettings.TryGetValue("volume", out var volume) ? volume : RemoteController.DEFAULT_VOLUME)}");
            sb.AppendLine($"IsOnline {_remoteController.IsOnline}");
            sb.AppendLine($"Brightness {(_remoteController.CurrentSettings.TryGetValue("brightness", out var brightness) ? brightness : RemoteController.DEFAULT_BRIGHTNESS)}");
            sb.AppendLine($"Contrast {(_remoteController.CurrentSettings.TryGetValue("contrast", out var contrast) ? contrast : RemoteController.DEFAULT_CONTRAST)}");

            return sb.ToString();
        }
    }
}