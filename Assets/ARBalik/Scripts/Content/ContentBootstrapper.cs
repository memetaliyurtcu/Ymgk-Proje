using System.Collections;
using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARFishing.Content
{
    public class ContentBootstrapper : MonoBehaviour
    {
        [SerializeField] CreatureDatabase m_CreatureDatabase;
        [SerializeField] AccessibilitySettings m_AccessibilitySettings;
        [SerializeField] LocalizationTable m_LocalizationTable;
        [SerializeField] Locale m_DefaultLocale = Locale.Turkish;
        [SerializeField] string m_ActivitySceneName = "Activity";

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);

            if (m_CreatureDatabase == null)
            {
                Debug.LogError("[ContentBootstrapper] CreatureDatabase not assigned. Aborting.");
                yield break;
            }

            ServiceLocator.Register(m_CreatureDatabase);

            if (m_AccessibilitySettings != null) AccessibilityState.Current = m_AccessibilitySettings;
            if (m_LocalizationTable != null) Localizer.Table = m_LocalizationTable;
            Localizer.Active = m_DefaultLocale;

            var op = SceneManager.LoadSceneAsync(m_ActivitySceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone) yield return null;

            yield return null;

            if (ServiceLocator.TryGet<ActivityController>(out var controller))
            {
                controller.TryTransition(ActivityState.Idle);
            }
            else
            {
                Debug.LogWarning("[ContentBootstrapper] ActivityController missing in Activity scene; FSM stuck in Bootstrap.");
            }

            Destroy(gameObject);
        }
    }
}
