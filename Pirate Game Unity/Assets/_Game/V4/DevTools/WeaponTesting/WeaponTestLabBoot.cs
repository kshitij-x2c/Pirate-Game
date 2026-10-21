using System.Collections;
using UnityEngine;

namespace RoguePirates.V4.DevTools.WeaponTesting
{
    /// <summary>
    /// V5 Weapon Lab: the one object in Scenes/V4/WeaponLab. On Play it loads the lab's voyage (the real run scene,
    /// <see cref="WeaponTestLab.Load"/>) and starts the lab in it. It outlives its own scene for the load, then goes.
    /// </summary>
    public sealed class WeaponTestLabBoot : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Go());
        }

        IEnumerator Go()
        {
            var load = WeaponTestLab.Load();
            while (true)
            {
                object current;
                try
                {
                    if (!load.MoveNext()) break;
                    current = load.Current;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    break;
                }
                yield return current;
            }
            Destroy(gameObject);
        }
    }
}
