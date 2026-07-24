using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.UI;

namespace GTAAnimalPacifistMod
{
    public class AnimalPacifist : Script
    {
        private bool initialized = false;

        private readonly Keys initKey = Keys.F2;
        private readonly Keys testSpawnKey = Keys.F3;
        private readonly Player player = Game.Player;

        private readonly List<Ped> cougarPool = new();
        private readonly HashSet<Ped> trackedAnimals = new();
        private readonly Random random = new();

        private RelationshipGroup cougarRelGroup;

        public AnimalPacifist()
        {
            Notify($"Press ~y~{initKey}~w~ to toggle Animal Pacifist.");
            Notify($"Press ~y~{testSpawnKey}~w~ to manually test cougar spawn.");
            KeyDown += OnKeyDown;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            cougarRelGroup = World.AddRelationshipGroup("AGGRESSIVE_COUGARS");

            Tick += OnTick;
            Notify("~g~Animal Pacifist Enabled");
        }

        private void Deinitialize()
        {
            if (!initialized) return;
            initialized = false;

            Tick -= OnTick;
            CleanupPool(true);
            trackedAnimals.Clear();

            Notify("~r~Animal Pacifist Disabled");
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == initKey)
            {
                InitKeyPress();
            }
            else if (e.KeyCode == testSpawnKey)
            {
                if (!initialized)
                {
                    Notify("~r~Enable the script first with " + initKey);
                    return;
                }

                Notify("~y~DEBUG: Attempting manual spawn...");
                SpawnCougars(1);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!initialized) return;

            if (player.Character.IsDead)
            {
                CleanupPool(true);
                trackedAnimals.Clear();
                return;
            }

            CleanupPool();
            CheckAnimals();
        }

        private void CheckAnimals()
        {
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Character, 100f);

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null || !ped.Exists() || ped.IsHuman)
                    continue;

                if (cougarPool.Contains(ped))
                    continue;

                trackedAnimals.Add(ped);

                if (ped.IsDead)
                {
                    if (ped.Killer == player.Character || ped.HasBeenDamagedBy(player.Character))
                    {
                        Notify("~r~YOU KILLED AN ANIMAL. PREPARE TO DIE.");
                        trackedAnimals.Remove(ped);
                        ped.Delete(); 

                        SpawnCougars(10);
                    }
                }
            }

            trackedAnimals.RemoveWhere(p => p == null || !p.Exists());
        }

        private void SpawnCougars(int amount)
        {
            Model cougarModel = new(PedHash.MountainLion);

            if (!cougarModel.IsValid)
            {
                Notify("~r~DEBUG ERROR: Model is invalid!");
                return;
            }

            cougarModel.Request(5000);

            if (!cougarModel.IsLoaded)
            {
                Notify("~r~DEBUG ERROR: Model failed to load in time!");
                return;
            }

            cougarRelGroup.SetRelationshipBetweenGroups(player.Character.RelationshipGroup, Relationship.Hate);
            player.Character.RelationshipGroup.SetRelationshipBetweenGroups(cougarRelGroup, Relationship.Hate);

            Vector3 spawnCenter = player.Character.Position + (player.Character.ForwardVector * 12f);
            int successfulSpawns = 0;

            for (int i = 0; i < amount; i++)
            {
                Vector3 offsetPos = spawnCenter + new Vector3(
                    (float)(random.NextDouble() * 8 - 4),
                    (float)(random.NextDouble() * 8 - 4),
                    0f
                );

                World.GetGroundHeight(new Vector3(offsetPos.X, offsetPos.Y, offsetPos.Z), out float groundZ , GetGroundHeightMode.Normal);
                Vector3 finalSpawnPos = new(offsetPos.X, offsetPos.Y, groundZ + 1.0f);

                Ped cougar = Ped.Create(cougarModel, finalSpawnPos);

                if (cougar != null && cougar.Exists())
                {
                    cougar.RelationshipGroup = cougarRelGroup;
                    successfulSpawns++;
                    cougarPool.Add(cougar);

                    cougar.SetCombatAttribute(CombatAttributes.Aggressive, true);
                    cougar.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
                    cougar.SetCombatAttribute(CombatAttributes.FleesFromInvincibleOpponents, false);
                    cougar.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);

                    cougar.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                    cougar.Task.ClearAll();
                    cougar.Task.Combat(player.Character, TaskCombatFlags.PreventChangingTarget, TaskThreatResponseFlags.None);
                }
            }

            Notify($"~g~DEBUG: Successfully spawned {successfulSpawns} cougars.");
            cougarModel.MarkAsNoLongerNeeded();
        }

        private void CleanupPool(bool clear = false)
        {
            if (clear)
            {
                foreach (Ped cougar in cougarPool)
                {
                    if (cougar != null && cougar.Exists())
                        cougar.Delete();
                }
                cougarPool.Clear();
                return;
            }

            cougarPool.RemoveAll(c => c == null || !c.Exists() || !c.IsAlive);
        }

        private void InitKeyPress()
        {
            if (initialized) Deinitialize();
            else Initialize();
        }

        private void Notify(string text)
        {
            Notification.PostTicker(text, false);
        }
    }
}
