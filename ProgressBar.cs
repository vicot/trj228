using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrostWind.Utils
{
    public class ProgressBar : SingletonMonoBehaviour<ProgressBar>
    {
        [SerializeField] private GameObject GameOverScreen;
        [SerializeField] private GameObject GameOverScreenText;
        [SerializeField] private GameObject GameOverScreenButton;
        [SerializeField] private Transform fill;
        [SerializeField] private TMP_Text scoreText;

        [SerializeField] private float maxValue = 7.5f;
        [SerializeField, Range(0f, 1f)] private float progress = 0.3f;
        [SerializeField] private float speed = 0.1f;
        [SerializeField] private float speedIncrease = 0.01f;
        [SerializeField] private float speedDecrease = 0.8f;
        [SerializeField] private float speedIncreaseTime = 1f;
        [SerializeField] private float maxCapacity = 100f;
        [SerializeField] private float capacityIncrease = 5f;
        [SerializeField] private float cooldownPower = 0.1f;

        [SerializeField] private float usedCapacity = 0f;

        public bool IsOverheating => progress > 0.1f;
        public bool IsGameOver { get; private set; }

        private int _score = 0;
        private float _start;

        private void Start()
        {
            _start = Time.time;
        }

        private void Update()
        {
            if (IsGameOver) return;
            var time = Time.time;

            if (time - _start > speedIncreaseTime)
            {
                speed  *= (1f + speedIncrease);
                _start =  time;
            }

            usedCapacity += speed * Time.deltaTime;
            bool wasOverheat = IsOverheating;
            progress     =  Mathf.Clamp01(usedCapacity / maxCapacity);
            if (wasOverheat != IsOverheating)
            {
                if(IsOverheating) GridManager.Instance.FadeBossMusic();
                else  GridManager.Instance.FadeNormalMusic();
            }
            
            var scale = fill.localScale;
            scale.x         = progress * maxValue;
            fill.localScale = scale;

            scoreText.text = _score.ToString();
            if (progress >= 1.0f)
            {
                EndGame();
            }

            if (progress > 0.2f) speedIncrease =1f;
        }

        public void IncreaseCapacity() => maxCapacity += capacityIncrease;
        public void ReduceSpeed() => speed *= speedDecrease;

        public void CoolDown(float score)
        {
            usedCapacity -= score;
            usedCapacity =  MathF.Max(0, usedCapacity);
        }

        public void AddScore(int destroyed, int combo)
        {
            _score += destroyed * combo * 10;
            CoolDown(cooldownPower * destroyed * combo);
        }

        public void EndGame()
        {
            GameOverScreen.SetActive(true);
            GameOverScreenText.SetActive(true);
            GameOverScreenButton.SetActive(true);
            IsGameOver = true;
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}