using System;
using GTA.UI;

namespace Wanted_Level_Restyle_2
{
    class WlrException : Exception // Exception related to this mod.
    {
        public bool IsFatal; // If true, the two scripts will be aborted.

        public WlrException(string notification, bool fatal) : base(notification)
        {
            IsFatal = fatal;
            ShowMessage(Message, fatal);
        }

        public static void ShowMessage(string message, bool fatal)
        {
            if (fatal)
            {
                if (message != null && message != string.Empty)
                {
                    Notification.Show("~r~ERROR~w~\r\n" + message + "\r\n~r~SCRIPTS ABORTED~w~.");
                }
            }
            else
            {
                if (message != null && message != string.Empty)
                {
                    Notification.Show("~y~WARNING~w~\r\n" + message);
                }
            }
        }
    }
}
