// Keys: https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=netframework-4.8

using GTA;
using GTA.UI;
using System.Windows.Forms;

namespace gta_money_mod
{
    public class Mod : Script
    {
        readonly int _key = 118; // Keys.F7
        public Mod()
        {
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.GetHashCode() == GetKeyHash(_key))
            {
                Game.Player.Money = 2147483647; // 2.14 billion
                Notify("Set money to 2.14 billion!");
            }
        }

        private static int GetKeyHash(Keys key)
        {
            return key.GetHashCode();
        }

        private static int GetKeyHash(int key)
        {
            return key;
        }

        private static void Notify(string text)
        {
            Notification.PostTicker(text, false, false);
        }
    }
}
