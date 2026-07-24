using GTA;
using GTA.UI;
using System;
using System.Windows.Forms;

namespace GTAHumanPacifistMod;

public class HumanPacifist : Script
{
    bool initialized = false;
    Keys initKey = Keys.F1;

    public HumanPacifist()
    {
        Notify($"Human pacifist loaded! Press {initKey} to toggle!");
        KeyDown += OnKeyDown;
    }

    private void OnTick(object sender, EventArgs e)
    {

    }

    private void Initialize()
    {
        initialized = true;
        Tick += OnTick;
    }

    private void Deinitialize()
    {
        initialized = false;
        Tick += null;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == initKey)
        {
            if (initialized)
            {
                Deinitialize();
            }
            else
            {
                Initialize();
            }
        }
    }

    private void Notify(string text)
    {
        Notification.PostTicker(text, false);
    }
}
