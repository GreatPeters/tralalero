using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class FTUE_script : MonoBehaviour
    {
        [Header("FTUE Panel Settings")]
        [SerializeField] private GameObject ftue_parent;
        [SerializeField] private Sprite[] iconList;
        [SerializeField] private GameObject ftue_icon;
        [SerializeField] private TextMeshProUGUI ftue_body;

        // private string[] titles = { "Movement", "Enemies", "Weapon Barrels", "Power Ups" };
        private string[] bodies = {"Slide your finger across the screen to move the player",
                               "Align your line of fire to damage enemies",
                               "Destroy the weapon barrels to switch weapons",
                               "Receive power ups or negative effects" };

        private Animator animator;
        private List<bool> hasBeenSeen = new List<bool> { false, false, false, false };
        private bool ftueIsActive = false;
        private bool isTutorialDone;
        private Image iconImage;

        private void Start()
        {
            isTutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1; // 0 - false, 1 - true
            if (isTutorialDone)
            {
                Destroy(ftue_parent);
                Destroy(gameObject);
                return;
            }

            animator = ftue_parent.GetComponentInChildren<Animator>();
            iconImage = ftue_icon.GetComponent<Image>();
        }


        void OnTriggerEnter(Collider other)
        {
            if (TimeManager.isGameRunning == false) return;
            if (isTutorialDone) return;

            if (other.CompareTag("EnemyTag") && !hasBeenSeen[1]) { StartCoroutine(ShowDisplay(1, 2)); }             // first enemy to contact trigger
            else if (other.CompareTag("BarrelTag") && !hasBeenSeen[2]) { StartCoroutine(ShowDisplay(2, 3)); }       // first barrel to contact trigger
            else if (other.CompareTag("WallTag") && !hasBeenSeen[3])                                                // first wall to contact trigger
            {
                StartCoroutine(ShowDisplay(3, 3));
                PlayerPrefs.SetInt("TutorialDone", 1);
            }
        }

        public IEnumerator ShowDisplay(int index, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (ftueIsActive == true || TimeManager.isGameRunning == false) yield break;

            ftueIsActive = true;
            hasBeenSeen[index] = true;
            // Function to trigger the display of a specific FTUE child based on the index
            ftue_parent.SetActive(true);

            // ftue_title.text = titles[index];
            iconImage.sprite = iconList[index];
            ftue_body.text = bodies[index];

            animator.SetTrigger("PopIn");
            StartCoroutine(HideAfterDelay());
        }

        // Coroutine to hide the FTUE screen after a specified delay
        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(3.5f);
            ftue_parent.SetActive(false);
            ftueIsActive = false;
        }

    }
}