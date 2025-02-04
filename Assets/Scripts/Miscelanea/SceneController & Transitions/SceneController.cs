﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DefinitiveScript 
{
    public class SceneController : MonoBehaviour
    {
        private bool created;
        private PlayerBehaviour PlayerBehaviour;
        private PlayerHealthController PlayerHealthController;
        private Transform BoatTransform;

        public Image blackScreen;
        public TextMeshProUGUI deathText;
        public GameObject scoreScreen;

        public float fadingTime = 0.5f;

        private int lastScene;
        private int dockID;

        private DockController[] BoatDocks;
        private DockController[] IslandDocks;

        private Transform boatInitialPoint;
        private Transform exitFromCavernSpawnPoint;
        private Transform enterIntoCavernSpawnPoint;

        private int mainMenuID = 0;
        private int boatSceneID = 1;
        private int islandSceneID = 2;
        private int cavernSceneID = 3;

        public float mainMenuLODBias = 1.5f;
        public float boatSceneLODBias = 3.5f;
        public float islandSceneLODBias = 1.5f;
        public float cavernSceneLODBias = 2f;

        private GeneralSoundController soundController;

        private GameData gameData;

        private float gameTimer;
        private bool startedTimer;

        public AudioSource gameOverSource;

        void Awake()
        {
            gameTimer = 0f;
            startedTimer = false;

            GameObject[] objs = GameObject.FindGameObjectsWithTag("SceneController");

            if(objs.Length > 1 && !created)
            {
                Destroy(this.gameObject);
            }
            else
            {   
                //if(!created) lastScene = -1;
                lastScene = SceneManager.GetActiveScene().buildIndex;

                DontDestroyOnLoad(GameManager.Instance.GameObject);
                DontDestroyOnLoad(this.gameObject);

                SceneManager.sceneLoaded += InitializeScene;

                GameManager.Instance.SceneController = this;

                created = true;

                gameData = new GameData();
            }
        }

        void Start()
        {
            if(SceneManager.GetActiveScene().buildIndex == mainMenuID) GameManager.Instance.CursorController.UnlockCursor();
            else GameManager.Instance.CursorController.LockCursor();

            blackScreen.enabled = false;
            InitializeScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void Update()
        {
            if(startedTimer)
            {
                gameTimer += Time.deltaTime;
            }

            if(Input.GetKeyDown(KeyCode.T)) //DevHack
            {
                StartScoreScreen();
            }
        }

        private void FindPlayer()
        {
            GameObject aux = GameObject.Find("Player");
            if(aux != null)
            {
                PlayerBehaviour = aux.GetComponent<PlayerBehaviour>();
                
                PlayerHealthController = PlayerBehaviour.GetComponent<PlayerHealthController>();
                PlayerHealthController.LoadPlayerData();
            }
        }

        private void FindBoat()
        {
            GameObject aux = GameObject.Find("Boat");
            if(aux != null) BoatTransform = aux.transform;
        }

        private void FindBoatInitialPoint()
        {
            GameObject aux = GameObject.Find("BoatInitialPoint");
            if(aux != null) boatInitialPoint = aux.transform;
        }

        private void FindBoatDocks()
        {
            GameObject[] aux = GameObject.FindGameObjectsWithTag("BoatDock");
            BoatDocks = new DockController[aux.Length];
            for(int i = 0; i < aux.Length; i++)
            {
                BoatDocks[i] = aux[i].GetComponent<DockController>();
            }
        }

        private void FindIslandDocks()
        {
            GameObject[] aux = GameObject.FindGameObjectsWithTag("PlayerDock");
            IslandDocks = new DockController[aux.Length];
            for(int i = 0; i < aux.Length; i++)
            {
                IslandDocks[i] = aux[i].GetComponent<DockController>();
            }
        }

        private void FindExitCavernSpawnPoint()
        {
            GameObject aux = GameObject.Find("ExitFromCavernSpawnPoint");
            if(aux != null) exitFromCavernSpawnPoint = aux.transform;
        }

        private void FindEnterCavernSpawnPoint()
        {
            GameObject aux = GameObject.Find("EnterIntoCavernSpawnPoint");
            if(aux != null) enterIntoCavernSpawnPoint = aux.transform;
        }

        private MapUIController FindMapUI()
        {
            GameObject aux = GameObject.FindWithTag("MapUI");
            return aux.GetComponent<MapUIController>();
        }

        private void InitializeScene(Scene scene, LoadSceneMode mode)
        {
            deathText.enabled = false;        

            StartCoroutine(InitializeSceneCoroutine(scene));

            soundController = GameObject.FindWithTag("SoundController").GetComponent<GeneralSoundController>();
        }

        private IEnumerator InitializeSceneCoroutine(Scene scene)
        {
            if(lastScene != -1)
            {
                if(scene.buildIndex == mainMenuID)
                {
                    QualitySettings.lodBias = mainMenuLODBias;

                    ResetGameTimer();
                    GameManager.Instance.CursorController.UnlockCursor();
                    yield return StartCoroutine(FadeIn(fadingTime));
                }
                else if(scene.buildIndex == boatSceneID)
                {
                    QualitySettings.lodBias = boatSceneLODBias;

                    GameManager.Instance.CursorController.LockCursor();

                    FindBoatInitialPoint();
                    FindBoat();
                    FindBoatDocks();
                    
                    StartGameTimer();

                    if(lastScene == mainMenuID)
                    {
                        BoatTransform.position = boatInitialPoint.position;
                    }
                    else if(lastScene == islandSceneID)
                    {
                        Transform dockDestinationPoint = null;
                        for(int i = 0; i < BoatDocks.Length; i++)
                        {
                            if(BoatDocks[i].dockID == dockID)
                            {
                                dockDestinationPoint = BoatDocks[i].boatSpawnPoint;
                                break;
                            } 
                        }
                        BoatTransform.position = dockDestinationPoint.position;
                        BoatTransform.rotation = dockDestinationPoint.rotation;
                    }
                    
                    yield return StartCoroutine(FadeIn(fadingTime));
                }
                else if(scene.buildIndex == islandSceneID)
                {
                    QualitySettings.lodBias = islandSceneLODBias;

                    GameManager.Instance.CursorController.LockCursor();
                    FindPlayer();
                    FindIslandDocks();
                    FindExitCavernSpawnPoint();
                    FindBoat();
                    FindMapUI().ChangeBoatPosition(dockID);
                    
                    StartGameTimer();

                    PlayerBehaviour.stopInput = true;
                    if(lastScene == boatSceneID)
                    {
                        Vector3 dockDestinationPoint = Vector3.zero;
                        for(int i = 0; i < IslandDocks.Length; i++)
                        {
                            if(IslandDocks[i].dockID == dockID)
                            {
                                dockDestinationPoint = IslandDocks[i].playerSpawnPoint.position;
                                break;
                            } 
                        }
                        PlayerBehaviour.transform.position = dockDestinationPoint;
                    }
                    else if(lastScene == cavernSceneID)
                    {
                        PlayerBehaviour.transform.position = exitFromCavernSpawnPoint.position;
                    }

                    for(int i = 0; i < IslandDocks.Length; i++)
                    {
                        if(IslandDocks[i].dockID == dockID)
                        {
                            BoatTransform.position = IslandDocks[i].boatDockedPoint.position;
                            BoatTransform.rotation = IslandDocks[i].boatDockedPoint.rotation;
                        }
                    }

                    yield return StartCoroutine(FadeIn(fadingTime));

                    PlayerBehaviour.stopInput = false;
                }
                else if(scene.buildIndex == cavernSceneID)
                {
                    QualitySettings.lodBias = cavernSceneLODBias;

                    GameManager.Instance.CursorController.LockCursor();
                    FindPlayer();
                    FindEnterCavernSpawnPoint();
                    
                    StartGameTimer();

                    if(lastScene == islandSceneID || lastScene == cavernSceneID)
                    {   
                        PlayerBehaviour.stopInput = true;

                        PlayerBehaviour.transform.position = enterIntoCavernSpawnPoint.position;

                        yield return StartCoroutine(FadeIn(fadingTime));

                        PlayerBehaviour.stopInput = false;
                    }
                }
            }
            
        }

        private IEnumerator ChangeToScene(int sceneID)
        {
            soundController.FadeOutTheme();

            lastScene = SceneManager.GetActiveScene().buildIndex;
            yield return StartCoroutine(FadeOut(fadingTime));

            SceneManager.LoadScene(sceneID);
        }

        public void BackToMenu()
        {
            ResetGameData();
            StartCoroutine(ChangeToScene(mainMenuID));
        }

        public void StartGame()
        {
            StartCoroutine(ChangeToScene(boatSceneID));
        }

        public void FinishGame()
        {
            scoreScreen.SetActive(false);
            ResetGameData();
            SceneManager.LoadScene(mainMenuID);
        }

        public void DockTheBoat(int dockID)
        {
            this.dockID = dockID;

            StartCoroutine(ChangeToScene(islandSceneID));
        }

        public void ToSail(int dockID)
        {
            PlayerHealthController.SavePlayerData();
            this.dockID = dockID;

            StartCoroutine(ChangeToScene(boatSceneID));
        }

        public void EnterIntoTheCavern()
        {
            PlayerHealthController.SavePlayerData();
            StartCoroutine(ChangeToScene(cavernSceneID));
        }

        public void ExitFromTheCavern()
        {
            PlayerHealthController.SavePlayerData();
            StartCoroutine(ChangeToScene(islandSceneID));
        }

        public void PlayDeath()
        {
            ResetGameData();

            deathText.enabled = true;
            StartCoroutine(FadeInDeathText(1.5f));

            soundController.FadeOutTheme();
        }

        private IEnumerator FadeInDeathText(float time)
        {
            Color c = deathText.color;
            c.a = 0f;
            deathText.color = c;

            float initialAlpha = 0f;
            float finalAlpha = 1f;

            RectTransform deathTextTransform = deathText.GetComponent<RectTransform>();

            Vector3 finalScale = deathTextTransform.localScale;
            Vector3 initialScale = finalScale * 0.5f;

            deathText.GetComponent<RectTransform>().localScale = initialScale;

            float elapsedTime = 0.0f;

            while(elapsedTime < time)
            {
                elapsedTime += Time.deltaTime;

                c.a = Mathf.Lerp(initialAlpha, finalAlpha, elapsedTime / time);
                deathText.color = c;

                deathTextTransform.localScale = Vector3.Lerp(initialScale, finalScale, elapsedTime / time);

                yield return null;
            }

            c.a = finalAlpha;
            deathText.color = c;

            deathTextTransform.localScale = finalScale;
            
            gameOverSource.Play();
        }

        public void PlayerDead()
        {
            StartCoroutine(ChangeToScene(SceneManager.GetActiveScene().buildIndex));
        }

        public void StartScoreScreen()
        {
            GameManager.Instance.CursorController.UnlockCursor();
            PauseGameTimer();

            soundController.FadeOutTheme();

            StartCoroutine(FadeOut(1.0f));

            scoreScreen.SetActive(true);
            scoreScreen.GetComponent<ScoreScreenController>().Play(1.0f, PlayerHealthController.GetCurrentMoney(), gameTimer);
        }

        private IEnumerator FadeOut(float time)
        {
            blackScreen.enabled = true;
            float initialAlpha = 0f;
            float finalAlpha = 1f;

            float elapsedTime = 0.0f;

            Color c = blackScreen.color;
            while(elapsedTime < time)
            {
                elapsedTime += Time.deltaTime;

                c.a = Mathf.Lerp(initialAlpha, finalAlpha, elapsedTime / time);
                blackScreen.color = c;

                yield return null;
            }
            c.a = finalAlpha;
            blackScreen.color = c;
        }

        private IEnumerator FadeIn(float time)
        {
            float initialAlpha = 1f;
            float finalAlpha = 0f;

            float elapsedTime = 0.0f;

            Color c = blackScreen.color;
            while(elapsedTime < time)
            {
                elapsedTime += Time.deltaTime;

                c.a = Mathf.Lerp(initialAlpha, finalAlpha, elapsedTime / time);
                blackScreen.color = c;

                yield return null;
            }

            c.a = finalAlpha;
            blackScreen.color = c;
            blackScreen.enabled = false;
        }

        public float LoadPlayerHealth()
        {
            return gameData.playerHealth;
        }

        public int LoadPlayerMoney()
        {
            return gameData.playerMoney;
        }

        public bool LoadPlayerHasKey()
        {
            return gameData.playerHasKey;
        }

        public void SavePlayerData(float health, int money, bool hasKey)
        {
            gameData.playerHealth = health;
            gameData.playerMoney = money;
            gameData.playerHasKey = hasKey;
        }

        public bool GetResolvedVisualPuzle(int i)
        {
            return gameData.resolvedVisualPuzles[i];
        }

        public void ResolvedVisualPuzle(int i)
        {
            gameData.resolvedVisualPuzles[i] = true;
        }

        public bool GetResolvedPuzle(int i)
        {
            return gameData.resolvedPuzles[i];
        }
        
        public void ResolvedPuzle(int i)
        {
            gameData.resolvedPuzles[i] = true;
        }

        public bool GetOpennedCavern()
        {
            return gameData.opennedCavern;
        }

        public void OpennedCavern()
        {
            gameData.opennedCavern = true;
        }

        public void ResetGameData()
        {
            gameData.Reset();
        }

        public void StartGameTimer()
        {
            startedTimer = true;
        }

        public void PauseGameTimer()
        {
            startedTimer = false;
        }

        public void ResetGameTimer()
        {
            startedTimer = false;
            gameTimer = 0.0f;
        }

        public float GetGameTimer()
        {
            return gameTimer;
        }
    }
}