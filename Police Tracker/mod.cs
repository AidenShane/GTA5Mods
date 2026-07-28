using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Windows.Forms;

namespace GTAPoliceTrackerMod;

public class PoliceTracker : Script
{
    private const Keys _initKey = Keys.F6;

    bool _initialized = false;
    bool _useAbbreviations = true;
    string _lastCrime = "";
    Player _player = Game.Player;
    private string _pendingCrime = "";

    private int _lastWantedLevel = 0;
    private int _playerKillCount = 0;

    private string _lastAgency = "";
    private string _agency = "";

    private readonly HashSet<int> _deadPeds = [];

    public event EventHandler<CrimeChangedEventArgs> CrimeChanged;

    public PoliceTracker()
    {
        KeyDown += OnKeyDown;
        Notify($"Police tracker loaded, press {_initKey} to toggle the mod!");
    }

    private void Initialize()
    {
        if (_initialized)
        {
            Console.WriteLine("Attempt to run PoliceTracker.Initialize but mod is initialized!");
            return;
        }

        _initialized = true;

        CrimeChanged += OnCrimeChanged;
        Tick += OnTick;

        Notify("~g~Initialized Police Tracker!");
    }

    private void Deinitialize()
    {
        if (!_initialized)
        {
            Console.WriteLine("Attempt to run PoliceTracker.Deinitialize but mod isn't initialized!");
            return;
        }

        _initialized = false;

        CrimeChanged += null;
        Tick += null;

        Notify("~r~Deinitialized Police Tracker!");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == _initKey)
        {
            InitKeyPressed();
        }
    }

    private void InitKeyPressed()
    {
        if (_initialized)
        {
            Deinitialize();
        }
        else
        {
            Initialize();
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (!_initialized) return;

        string agency = _agency;
        string currAgency = GetAgency();

        int wantedLevel = _player.Wanted.WantedLevel;

        if (wantedLevel > 0)
        {
            CrimeChanged += OnCrimeChanged;
        }
        else
        {
            CrimeChanged += null;
            _lastCrime = "";
            _playerKillCount = 0;
        }

        if (wantedLevel > _lastWantedLevel && _pendingCrime != "")
        {
            SetCrime(_pendingCrime);
            _pendingCrime = "";
        }

        _lastWantedLevel = wantedLevel;
        
        if (_agency != currAgency)
        {
            _lastAgency = _agency;
            _agency = currAgency;
        }

        foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character, 100f))
        {
            if (ped.IsDead && _deadPeds.Add(ped.Handle))
            {
                OnPedDeath(ped);
            }
            else
            {
                OnPedCrime(ped);
            }
        }
    }

    private void OnPedDeath(Ped ped)
    {
        Entity killer = ped.Killer;
        

        if (killer == null)
            return;

        bool playerKilled =
            killer.Handle == _player.Character.Handle ||
            (_player.Character.IsInVehicle() &&
             killer.Handle == _player.Character.CurrentVehicle.Handle);

        if (!playerKilled)
            return;

        _playerKillCount++;

        string crime;

        if (_playerKillCount >= 2)
        {
            crime = "Homicide";
        }
        else
        {
            crime = "Murder";
        }

        if (ped.PedType == PedType.Cop)
        {
            crime = $"~r~{crime} Of a Police Officer~s~";
        }

        _pendingCrime = crime;
    }

    private void OnPedCrime(Ped ped)
    {
        if (ped.IsDead)
        {
            return;
        }

        if (ped.IsOnFire)
        {
            string crime = "Arson";

            if (ped.PedType == PedType.Cop)
            {
                crime = $"~r~{crime} Of a Police Officer~s~";
            }

            SetCrime(crime);
            return;
        }

        if (ped.HasBeenDamagedBy(_player.Character) && !ped.IsDead)
        {
            string crime = "Assault";

            if (ped.PedType == PedType.Cop)
            {
                crime = $"~r~{crime} Of a Police Officer~s~";
            }

            SetCrime(crime);
        }
    }
    private void SetCrime(string crime)
    {
        if (_lastCrime == crime) return;

        CrimeChanged?.Invoke(this, new CrimeChangedEventArgs(crime, _lastCrime));

        _lastCrime = crime;
        Console.WriteLine($"Setting Last crime to {crime}");
    }

    private void OnCrimeChanged(object sender, CrimeChangedEventArgs e)
    {
        if (e.OldCrime == "" || _lastAgency != GetAgency())
        {
            Notify($"You're now wanted by the ~b~{GetAgency()}~s~ for ~r~{e.NewCrime}~s~");
        }
        else
        {
            Notify($"You're now wanted for ~r~{e.NewCrime}~s~");
        }
    }

    private string GetAgency()
    {
        string region = GetRegion();
        string agency;

        if (_useAbbreviations)
        {
            agency = region switch
            {
                "Los Santos" => "LSPD",
                "Blaine County" => "BCSO",
                _ => "SAHP"
            };
        }
        else
        {
            agency = region switch
            {
                "Los Santos" => "Los Santos Police Department",
                "Blaine County" => "Blaine County Sheriffs Office",
                _ => "San Andreas Highway Patrol"
            };
        }

        if (IsWantedByMilitary())
        {
            agency = "Merryweather";
        }

        if (IsWantedByNoose())
        {
            if (_useAbbreviations)
            {
                agency = "NOOSE";
            }
            else
            {
                agency = "National Office Of Security Enforcement";
            }
        }

        return agency;
    }

    private bool IsWantedByNoose()
    {
        return _player.Wanted.WantedLevel > 4 && IsWantedByMilitary() == false;
    }

    private bool IsWantedByMilitary()
    {
        return Function.Call<string>(
            Hash.GET_NAME_OF_ZONE,
            _player.Character.Position.X,
            _player.Character.Position.Y,
            _player.Character.Position.Z
        ) == "ARMYB";
    }

    private void Notify(string text, bool important = false)
    {
        Notification.PostTicker(text, important);
    }

    private string GetRegion()
    {
        Vector3 pos = _player.Character.Position;

        string zone = Function.Call<string>(
            Hash.GET_NAME_OF_ZONE,
            pos.X,
            pos.Y,
            pos.Z
        );

        return zone switch
        {
            // los santos
            "AIRP" or "ALTA" or "BANNING" or "BEACH" or "BURTON" or
            "CHAMH" or "CYPRE" or "DAVIS" or "DELBE" or "DELPE" or
            "DOWNTOWN" or "DTVINE" or "EAST_V" or "ELYSIAN" or
            "HAWICK" or "KOREAT" or "LEGSQU" or "LMESA" or
            "LOSPUER" or "MIRR" or "MORN" or "PBOX" or
            "RANCHO" or "RICHM" or "ROCKF" or "SKID" or
            "STAD" or "STRAW" or "TEXTI" or "VCANA" or
            "VESP" or "VINE" or "WVINE"
                => "Los Santos",

            // blaine county
            "ALAMO" or "CHIL" or "CHU" or "DESRT" or "GREATC" or
            "GRAPES" or "HARMO" or "JAIL" or "MTCHIL" or
            "MTGORDO" or "PALETO" or "PALFOR" or "SANDY" or
            "SLAB" or "TATAMO" or "TONGVA" or "WINDF" or
            "ZANCUDO"
                => "Blaine County",

            _ => "Unknown"
        };
    }
}

public class CrimeChangedEventArgs(string _new, string _last)
{
    public string NewCrime { get; private set; } = _new;
    public string OldCrime { get; private set; } = _last;
}
