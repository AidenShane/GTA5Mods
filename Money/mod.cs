using GTA;
using GTA.UI;
using System.Windows.Forms;

namespace GtaMoneyMod;

public class MoneyMod : Script
{
    private const Keys _key = Keys.F7;
    private const int _amount = int.MaxValue;
    public MoneyMod()
    {
        KeyDown += OnKeyDown;
        SendInitNotif();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == _key)
        {
            Game.Player.Money = _amount;
            Notify($"Set money to {_amount:N0}");
        }
    }

    private void SendInitNotif()
    {
        Notify($"Loaded money mod, use {_key} to get ${_amount:N0}");
    }

    private static void Notify(string text)
    {
        Notification.PostTicker(text, false, false);
    }
}
