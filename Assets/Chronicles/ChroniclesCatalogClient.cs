using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MythicChronicles
{
    /// <summary>
    /// Optional public read-only backend probe for the offline Alpha.
    /// Never posts player saves, currencies, tokens or device identifiers.
    /// Real remote account linking and PvP remain explicitly disabled.
    /// </summary>
    public sealed class ChroniclesCatalogClient : MonoBehaviour
    {
        [Serializable]
        private sealed class RemoteHeroes
        {
            public int count;
            public string language;
            public bool liveGameEconomy;
        }

        [Serializable]
        private sealed class RemoteBanners
        {
            public string language;
            public bool liveSummoning;
        }

        public string Status { get; private set; } = "OFFLINE MODE";
        public bool Busy { get; private set; }

        public static bool TryPublicApiRoot(string address, out Uri endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(address) ||
                !Uri.TryCreate(address.Trim(), UriKind.Absolute, out Uri parsed))
                return false;
            if (!string.IsNullOrEmpty(parsed.UserInfo) ||
                !string.IsNullOrEmpty(parsed.Query) ||
                !string.IsNullOrEmpty(parsed.Fragment)) return false;
            if (parsed.Scheme != Uri.UriSchemeHttps)
            {
                #if UNITY_EDITOR
                if (parsed.Scheme != Uri.UriSchemeHttp || !parsed.IsLoopback)
                    return false;
                #else
                return false;
                #endif
            }
            endpoint = new Uri(parsed.GetLeftPart(UriPartial.Authority) + "/");
            return true;
        }

        public void CheckReadOnlyCatalog(string baseUrl)
        {
            if (Busy) return;
            Uri root;
            if (!TryPublicApiRoot(baseUrl, out root))
            {
                Status = string.IsNullOrWhiteSpace(baseUrl) ?
                    "OFFLINE MODE - NO SERVER CONFIGURED" :
                    "INVALID SERVER - HTTPS REQUIRED";
                return;
            }
            StartCoroutine(Probe(root));
        }

        private IEnumerator Probe(Uri root)
        {
            Busy = true;
            Status = "CHECKING READ-ONLY GAME DATA...";
            using (UnityWebRequest heroesRequest =
                UnityWebRequest.Get(new Uri(root, "v1/catalog/heroes")))
            {
                heroesRequest.timeout = 9;
                yield return heroesRequest.SendWebRequest();
                if (heroesRequest.result != UnityWebRequest.Result.Success)
                {
                    Status = "SERVER UNAVAILABLE - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
                RemoteHeroes heroes;
                try
                {
                    heroes = JsonUtility.FromJson<RemoteHeroes>(
                        heroesRequest.downloadHandler.text);
                }
                catch
                {
                    Status = "SERVER DATA INVALID - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
                if (heroes == null || heroes.count != 41 ||
                    heroes.language != "en-US" || heroes.liveGameEconomy)
                {
                    Status = "SERVER CATALOG MISMATCH - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
            }
            using (UnityWebRequest bannerRequest =
                UnityWebRequest.Get(new Uri(root, "v1/catalog/banners")))
            {
                bannerRequest.timeout = 9;
                yield return bannerRequest.SendWebRequest();
                if (bannerRequest.result != UnityWebRequest.Result.Success)
                {
                    Status = "BANNERS UNAVAILABLE - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
                RemoteBanners banners;
                try
                {
                    banners = JsonUtility.FromJson<RemoteBanners>(
                        bannerRequest.downloadHandler.text);
                }
                catch
                {
                    Status = "BANNER DATA INVALID - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
                if (banners == null || banners.language != "en-US" ||
                    banners.liveSummoning)
                {
                    Status = "BANNER CONTRACT MISMATCH - OFFLINE MODE";
                    Busy = false;
                    yield break;
                }
            }
            Status = "PUBLIC CATALOG ONLINE - GAMEPLAY STILL OFFLINE";
            Busy = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Busy = false;
        }
    }
}
