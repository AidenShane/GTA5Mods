using GTA;
using GTA.Math;
using GTA.UI;
using System;
using System.Linq;
using System.Windows.Forms;

namespace GTAHitmanMod
{
    class HitmanMod : Script
    {
        private readonly Player player = Game.Player;
        private readonly Random random = new();

        private bool initialized = false;

        private Ped target;
        private Blip blip;

        private int bounty;
        private int elapsed;

        public HitmanMod()
        {
            KeyDown += OnKeyDown;
            Notify("~g~Hitman mod loaded! Press F5 to toggle.");
        }

        private void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            Tick += OnTick;

            Notify("~g~Hitman mod enabled.");
        }

        private void Deinitialize()
        {
            if (!initialized)
                return;

            initialized = false;
            Tick -= OnTick;

            ClearTargetData();

            Notify("~r~Hitman mod disabled.");
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (initialized)
                    Deinitialize();
                else
                    Initialize();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (target != null && target.Exists())
            {
                if (target.IsDead)
                {
                    if (target.Killer != null &&
                        target.Killer.Exists() &&
                        target.Killer.Handle == player.Character.Handle)
                    {
                        OnTargetKilled();
                    }
                    else
                    {
                        OnFailed();
                    }
                }
            }
            else
            {
                elapsed++;

                if (elapsed >= 500)
                {
                    elapsed = 0;
                    GiveNewTarget();
                }
            }
        }

        private void GiveNewTarget()
        {
            target = GetRandomPed();

            if (target == null)
                return;

            bounty = random.Next(10000, 500000);

            blip = target.AddBlip();
            blip.Name = "Target";
            blip.Color = BlipColor.Red;
            blip.IsFriendly = false;
            blip.Scale = 0.8f;

            Notify($"~y~New target assigned. Reward: ${bounty:n0}");
        }

        private Ped GetRandomPed()
        {
            var peds = World.GetAllPeds()
                .Where(p =>
                    p != null &&
                    p.Exists() &&
                    p.IsHuman &&
                    !p.IsDead &&
                    !p.IsPlayer &&
                    p.Handle != player.Character.Handle)
                .ToArray();

            if (peds.Length == 0)
                return null;

            return peds[random.Next(peds.Length)];
        }

        private void OnTargetKilled()
        {
            Notify("~g~Target eliminated!");

            player.Money += bounty;

            Notify($"~g~Received ${bounty:n0}");

            ClearTargetData();
        }

        private void OnFailed()
        {
            Notify("~r~Target died without you killing them.");

            ClearTargetData();
        }

        private void ClearTargetData()
        {
            bounty = 0;
            elapsed = 0;

            if (target != null && target.Exists())
            {
                target.MarkAsNoLongerNeeded();
                target = null;
            }

            if (blip != null && blip.Exists())
            {
                blip.Delete();
                blip = null;
            }
        }

        private void Notify(string text)
        {
            Notification.PostTicker(text, false);
        }
    }
}
