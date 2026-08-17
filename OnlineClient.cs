using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace ClashOfDrayven
{
    internal static class AppConfig
    {
        public static string ServerBaseUrl
        {
            get
            {
                var value = Environment.GetEnvironmentVariable("DRAYVEN_SERVER");
                return string.IsNullOrWhiteSpace(value) ? "http://irautox.ir:8456" : value.TrimEnd('/');
            }
        }
    }

    internal sealed class UserDto
    {
        public int id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
    }

    internal sealed class AuthResult
    {
        public bool Ok { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    internal sealed class AuthResponse
    {
        public bool ok { get; set; }
        public string token { get; set; }
        public UserDto user { get; set; }
        public string error { get; set; }
        public ServerPlayerState state { get; set; }
    }

    internal sealed class ProfileResponse
    {
        public bool ok { get; set; }
        public UserDto user { get; set; }
        public ServerClan clan { get; set; }
        public string error { get; set; }
        public bool Ok { get { return ok; } }
        public UserDto User { get { return user; } }
    }

    internal sealed class StateResponse
    {
        public bool ok { get; set; }
        public ServerPlayerState state { get; set; }
        public string error { get; set; }
    }

    internal sealed class ServerBuilding
    {
        public string instanceId { get; set; }
        public string definitionId { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int level { get; set; }
    }

    internal sealed class ServerClan
    {
        public int id { get; set; }
        public string name { get; set; }
        public string tag { get; set; }
        public string role { get; set; }
        public List<ServerClanMember> members { get; set; }
    }

    internal sealed class ServerClanMember
    {
        public string username { get; set; }
        public string role { get; set; }
    }

    internal sealed class ServerPlayerState
    {
        public int gold { get; set; }
        public int elixir { get; set; }
        public int gems { get; set; }
        public int xp { get; set; }
        public int level { get; set; }
        public List<ServerBuilding> buildings { get; set; }
        public Dictionary<string, int> units { get; set; }
        public ServerClan clan { get; set; }
        public string lastSavedUtc { get; set; }
    }

    internal sealed class ClanResponse
    {
        public bool ok { get; set; }
        public ServerClan clan { get; set; }
        public string error { get; set; }
    }

    internal sealed class ApiResponse
    {
        public bool ok { get; set; }
        public string error { get; set; }
    }

    internal sealed class DrayvenApiException : Exception
    {
        public int StatusCode { get; private set; }
        public DrayvenApiException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
    }

    internal sealed class DrayvenApiClient
    {
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 128 };
        public string BaseUrl { get; private set; }
        public string Token { get; set; }

        public DrayvenApiClient(string baseUrl)
        {
            BaseUrl = (baseUrl ?? "").TrimEnd('/');
        }

        public AuthResult Login(string username, string password)
        {
            var r = Request<AuthResponse>("POST", "/api/v1/login", new Dictionary<string, object>
            {
                ["username"] = username,
                ["password"] = password
            });
            if (!r.ok) throw new DrayvenApiException(400, Friendly(r.error));
            Token = r.token;
            return new AuthResult { Ok = true, Token = r.token, User = r.user };
        }

        public AuthResult Register(string username, string email, string password)
        {
            var r = Request<AuthResponse>("POST", "/api/v1/register", new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["password"] = password
            });
            if (!r.ok) throw new DrayvenApiException(400, Friendly(r.error));
            Token = r.token;
            return new AuthResult { Ok = true, Token = r.token, User = r.user };
        }

        public ProfileResponse GetProfile() { return Request<ProfileResponse>("GET", "/api/v1/profile", null); }
        public StateResponse GetState() { return Request<StateResponse>("GET", "/api/v1/state", null); }
        public ClanResponse GetClan() { return Request<ClanResponse>("GET", "/api/v1/clans/me", null); }

        public ClanResponse CreateClan(string name, string tag)
        {
            return Request<ClanResponse>("POST", "/api/v1/clans/create", new Dictionary<string, object>
            {
                ["name"] = name,
                ["tag"] = tag
            });
        }

        public void PutState(GameSave save, int xp, int level)
        {
            var buildings = save.Buildings.Select(b => (object)new Dictionary<string, object>
            {
                ["instanceId"] = b.InstanceId.ToString("N"),
                ["definitionId"] = b.DefinitionId,
                ["x"] = b.X,
                ["y"] = b.Y,
                ["level"] = b.Level
            }).ToList();
            var units = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in save.Units) units[pair.Key] = pair.Value;
            object clan = null;
            if (save.Clan != null)
            {
                clan = new Dictionary<string, object>
                {
                    ["name"] = save.Clan.Name,
                    ["tag"] = save.Clan.Tag
                };
            }
            var state = new Dictionary<string, object>
            {
                ["gold"] = save.Gold,
                ["elixir"] = save.Elixir,
                ["gems"] = save.Gems,
                ["xp"] = xp,
                ["level"] = level,
                ["buildings"] = buildings,
                ["units"] = units,
                ["clan"] = clan,
                ["lastSavedUtc"] = DateTime.UtcNow.ToString("o")
            };
            var response = Request<ApiResponse>("PUT", "/api/v1/state", new Dictionary<string, object> { ["state"] = state });
            if (!response.ok) throw new DrayvenApiException(400, Friendly(response.error));
        }

        public void Logout()
        {
            try { Request<ApiResponse>("POST", "/api/v1/logout", new Dictionary<string, object>()); }
            finally { Token = null; SessionStore.Clear(); }
        }

        private T Request<T>(string method, string path, object body)
        {
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + path);
            request.Method = method;
            request.Accept = "application/json";
            request.UserAgent = "ClashOfDrayven/3.0 Windows";
            request.Timeout = 12000;
            request.ReadWriteTimeout = 12000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (!string.IsNullOrWhiteSpace(Token)) request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(_json.Serialize(body));
                request.ContentType = "application/json; charset=utf-8";
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            }
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    return _json.Deserialize<T>(reader.ReadToEnd());
            }
            catch (WebException ex)
            {
                var http = ex.Response as HttpWebResponse;
                string text = "";
                if (http != null)
                {
                    try { using (var reader = new StreamReader(http.GetResponseStream(), Encoding.UTF8)) text = reader.ReadToEnd(); }
                    catch { }
                }
                string message = "Server connection failed.";
                try
                {
                    var parsed = _json.Deserialize<Dictionary<string, object>>(text);
                    if (parsed != null && parsed.ContainsKey("error")) message = Friendly(Convert.ToString(parsed["error"]));
                }
                catch { if (!string.IsNullOrWhiteSpace(text)) message = text; }
                throw new DrayvenApiException(http == null ? 0 : (int)http.StatusCode, message);
            }
        }

        private static string Friendly(string error)
        {
            switch ((error ?? "").Trim())
            {
                case "invalid_credentials": return "Wrong username/email or password.";
                case "username_or_email_exists": return "That username or email is already registered.";
                case "username_3_20_alnum_underscore": return "Username must be 3-20 letters, numbers or underscore.";
                case "invalid_email": return "Enter a valid email address.";
                case "password_8_128": return "Password must be at least 8 characters.";
                case "already_in_clan": return "You are already in a clan.";
                case "tag_exists": return "That clan tag is already used.";
                case "invalid_clan_name": return "Clan name must be 3-24 simple characters.";
                case "invalid_clan_tag": return "Clan tag must be 2-6 letters/numbers.";
                case "unauthorized": return "Session expired. Sign in again.";
                default: return string.IsNullOrWhiteSpace(error) ? "Server rejected the request." : error.Replace('_', ' ');
            }
        }
    }

    internal static class SessionStore
    {
        private static string DirectoryPath
        {
            get
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IrAutoX", "ClashOfDrayven");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        private static string TokenPath { get { return Path.Combine(DirectoryPath, "session.dat"); } }

        public static string LoadToken()
        {
            try
            {
                if (!File.Exists(TokenPath)) return null;
                var protectedBytes = Convert.FromBase64String(File.ReadAllText(TokenPath));
                var clear = System.Security.Cryptography.ProtectedData.Unprotect(protectedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clear);
            }
            catch { return null; }
        }

        public static void SaveToken(string token)
        {
            try
            {
                var clear = Encoding.UTF8.GetBytes(token ?? "");
                var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(clear, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                File.WriteAllText(TokenPath, Convert.ToBase64String(protectedBytes));
            }
            catch { }
        }

        public static void Clear()
        {
            try { if (File.Exists(TokenPath)) File.Delete(TokenPath); } catch { }
        }
    }

    internal static class OnlineStateBridge
    {
        public static int Xp { get; set; }
        public static int Level { get; set; } = 1;

        public static void DownloadInto(DrayvenApiClient api, GameSave save)
        {
            var response = api.GetState();
            if (response == null || !response.ok || response.state == null)
                throw new InvalidOperationException(response == null ? "Invalid server response." : response.error);
            var s = response.state;
            save.Gold = s.gold;
            save.Elixir = s.elixir;
            save.Gems = s.gems;
            Xp = s.xp;
            Level = System.Math.Max(1, s.level);
            save.Buildings.Clear();
            if (s.buildings != null)
            {
                foreach (var b in s.buildings)
                {
                    Guid id;
                    if (!Guid.TryParse(b.instanceId, out id)) id = Guid.NewGuid();
                    save.Buildings.Add(new PlacedBuilding
                    {
                        InstanceId = id,
                        DefinitionId = b.definitionId ?? "townhall",
                        X = b.x,
                        Y = b.y,
                        Level = System.Math.Max(1, b.level)
                    });
                }
            }
            if (!save.Buildings.Any(b => string.Equals(b.DefinitionId, "townhall", StringComparison.OrdinalIgnoreCase)))
                save.Buildings.Add(new PlacedBuilding { DefinitionId = "townhall", X = 9, Y = 9, Level = 1 });
            save.Units.Clear();
            if (s.units != null)
                foreach (var pair in s.units) save.Units[pair.Key] = pair.Value;
            if (s.clan != null)
                save.Clan = new ClanInfo { Name = s.clan.name ?? "Clan", Tag = s.clan.tag ?? "DRY" };
            save.LastSavedUtc = DateTime.UtcNow;
        }
    }
}
