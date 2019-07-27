using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassTheBomb : MonoBehaviour {

    public PlayerControllerGM gmPlayer;
    public PlayerController[] players;
    [HideInInspector] public PassTheBombUI UI;

    public bool gameStarted = false;
    public bool isBombInPlay = false;
	public bool canPlaceBomb = false;
    public Transform bombPositions;
    public Vector3 bombStartPosition;

    [SerializeField] GameObject bombPrefab;

    List<GameObject> bombsInPlay = new List<GameObject>();

    AudioSource audioSource;

	PlayerController lastLosingPlayer;

	//AI GM
	float aiGMtimeToFreeze = -1f;
	bool aiGMcanFreeze = false;

	[Header("Game Settings")]
	public int roundsPerGame = 5;
	public int currentRound;
	public int pointsPrSecond = 1;
	[SerializeField] AudioClip tickingBomb;
	[SerializeField] AudioClip explodingBomb;
	public BombType[] bombTypes;

	[Header("Debug")]
	public bool bombExploding = true;


    void Start() {
        gmPlayer = GetComponent<GameController>().gmPlayer;
        players = GetComponent<GameController>().players;
        UI = GetComponent<GameController>().UIController.PassTheBombUI.GetComponent<PassTheBombUI>();
		UI.Setup ();
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(StartGame());
    }

    void Update() {
        if (!gameStarted) return;


		if (!isBombInPlay && canPlaceBomb) {
            if (!gmPlayer) {
                if (Input.GetKeyDown(KeyCode.Space)) {
                    RestartRound();
                    AddBomb();
                }
            }
		} else {
			if (!gmPlayer) {
				if (aiGMtimeToFreeze < 0) {
					if (aiGMcanFreeze) {
						FreezePlayer ();
						aiGMtimeToFreeze = Random.Range (8f, 13f);
					} else {
						aiGMtimeToFreeze = Random.Range (2f, 4f);
						aiGMcanFreeze = true;
					}
				} else {
					aiGMtimeToFreeze -= Time.deltaTime;
				}
			}
		}
    }

	void FreezePlayer() {
		players[GetComponent<GameController> ().GetRandomPlayerIndex ()].GetComponent<PassTheBombPlayer>().Freeze();
	}

    IEnumerator StartGame() {
		currentRound = 1;
		gameStarted = true;
		canPlaceBomb = true;

		print ("StartGame");
        for(int i = 0; i < players.Length; i++) {
            PlayerController player = players[i];
            if (player != null) {
                player.GetComponent<PassTheBombPlayer>().enabled = true;
                player.GetComponent<PassTheBombPlayer>().playerUI = UI.playerUIWrapper.GetChild(i).GetComponent<PassTheBombPlayerUI>();
                player.GetComponent<PassTheBombPlayer>().playerUI.ShowUI(true);
            }
        }
        UI.gameObject.SetActive(true);

        if (gmPlayer != null) {
            gmPlayer.GetComponent<PassTheBombGM>().enabled = true;
        }

		GetComponent<GameController>().UIController.SetStatusText("Runde " + currentRound);

        yield return new WaitForSeconds(3);

        if (!gmPlayer) {
            AddBomb();
        }
    }

    public void AddBomb(int _playerIndex = -1, float _bombTime = -1f) {
        print("AddBomb");
		//RestartRound ();

        isBombInPlay = true;
		canPlaceBomb = false;

        List<int> playerIndexes = GetComponent<GameController>().GetPlayerIndexes();
        int playerIndexToGetBomb = (_playerIndex == -1) ? Random.Range(0, playerIndexes.Count) : _playerIndex;
        PlayerController playerGetBomb = (_playerIndex == -1) ? players[playerIndexes[playerIndexToGetBomb]] : players[playerIndexToGetBomb];
        playerGetBomb.GetComponent<PassTheBombPlayer>().ReceiveBomb();
        GetComponent<GameController>().UIController.SetStatusText(playerGetBomb.playername + " har bomben!");

        GameObject bomb = Instantiate(bombPrefab, bombStartPosition, Quaternion.identity);
		BombType bombType = bombTypes[Random.Range (0, bombTypes.Length)];
		bomb.GetComponent<PassTheBombBomb>().bombTime = (_bombTime == -1) ? Random.Range(bombType.MinimumBombTime, bombType.MaximumBombTime) : _bombTime;
        bombsInPlay.Add(bomb);
        PlaceBomb(bomb, playerGetBomb.index);

        audioSource.clip = tickingBomb;
        audioSource.loop = true;
        audioSource.Play();
    }

    void PlaceBomb(GameObject _bomb, int _playerIndex) {
        Vector3 position = bombPositions.GetChild(_playerIndex).transform.position;
        _bomb.GetComponent<PassTheBombBomb>().SetNewPosition(position);
    }

	public void SendBombToPlayer(int _fromIndex, int _toIndex) {
		if (players[_toIndex] == null || !isBombInPlay) {
			return;
		}
		GameObject bomb = null;
		for(int i = 0; i < bombsInPlay.Count; i++) {
			if (bombsInPlay[i].transform.position == bombPositions.GetChild(_fromIndex).transform.position) {
				bomb = bombsInPlay[i];
				break;
			}
		}

		if (bomb == null) return;  
		if (!bomb.GetComponent<PassTheBombBomb>().isStill)  return;
		if (players[_toIndex].GetComponent<PassTheBombPlayer>().isFreezed) return;
		PlaceBomb(bomb, _toIndex);

		players[_fromIndex].GetComponent<PassTheBombPlayer>().SentBomb();
		players[_toIndex].GetComponent<PassTheBombPlayer>().ReceiveBomb();
	}

    public void BlowBomb() {
        isBombInPlay = false;
        audioSource.Stop();
        audioSource.PlayOneShot(explodingBomb);
        for(int i = 0; i < players.Length; i++) {
            if (players[i] == null) continue;
            if (players[i].GetComponent<PassTheBombPlayer>().hasBomb) {
                players[i].GetComponent<PassTheBombPlayer>().BombExploded();
				lastLosingPlayer = players [i];
                break;
            }
        }
        GetComponent<GameController>().cameraController.ShakeCamera();
		StartCoroutine(EndRound ());
    }

	IEnumerator EndRound() {
		foreach(PlayerController player in players) {
			if (player == null) continue;
			player.GetComponent<PassTheBombPlayer>().EndRound();
		}
		yield return new WaitForSeconds (3f);

		// GetComponent<GameController> ().cameraController.RotateAroundPlayer (lastLosingPlayer.transform);

		yield return new WaitForSeconds (3f);

		ReadyForNewRound ();
	}

	void ReadyForNewRound() {
		currentRound++;
		if (currentRound > roundsPerGame) {
			EndGame ();
			return;
		}

		foreach(GameObject bomb in bombsInPlay) {
			Destroy(bomb);
		}
		bombsInPlay.Clear();
		canPlaceBomb = true;

		if (gmPlayer != null) {
			gmPlayer.GetComponent<PassTheBombGM>().Restart();	
		} else {
			aiGMcanFreeze = false;
			aiGMtimeToFreeze = -1f;
		}
		print ("ReadyForNewRound");

		if (currentRound == roundsPerGame) {
			GetComponent<GameController>().UIController.SetStatusText("Sidste  runde");
		} else {
			GetComponent<GameController>().UIController.SetStatusText("Runde " + currentRound);
		}

	}

    void RestartRound() {
		print ("RestartRound");
        foreach(PlayerController player in players) {
            if (player == null) continue;
            player.GetComponent<PassTheBombPlayer>().Restart();
        }


    }

	void EndGame() {
		print ("Spillet er slut");
	}

}
