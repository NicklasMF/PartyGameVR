﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class PassTheBombPlayer : MonoBehaviour {

    public bool hasBomb = false;
    public float totalPoints = 0;
    public float currentPoints = 0;
    public PassTheBombPlayerUI playerUI;

    [SerializeField] ParticleSystem particleSmoke;
    InputDevice inputDevice;
    PassTheBomb controller;
    int playerIndex;

	float bombVibHaving = 0.1f;
	[Range(0,1f)]
	public float bombVibExplode = 0f;

    void Start() {
        inputDevice = GetComponent<PlayerController>().Device;
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<PassTheBomb>();
        playerUI.SetUI(GetComponent<PlayerController>().playername, GetComponent<PlayerController>().color);
	}

    void Update() {
        if (hasBomb) {
			if (controller.isBombInPlay) {
                currentPoints += controller.pointsPrSecond * Time.deltaTime;
                playerUI.SetCurrentPoints(currentPoints);

                if (inputDevice.Action1.WasPressed) {
                    playerIndex = 0; // Kryds
                } else if (inputDevice.Action2.WasPressed) {
                    playerIndex = 1; // Rund
                } else if (inputDevice.Action3.WasPressed) {
                    playerIndex = 2; // Firkant
                } else if (inputDevice.Action4.WasPressed) {
                    playerIndex = 3; // Trekant
                } else {
                    playerIndex = -1;
                }

                if (playerIndex != -1 && GetComponent<PlayerController>().index != playerIndex) {
                    controller.SendBombToPlayer(GetComponent<PlayerController>().index, playerIndex);
                }
            } else {
                currentPoints = 0;
                playerUI.SetCurrentPoints(currentPoints);
            }
        }
    }

	public void BombExploded() {
        StartCoroutine(BombExplodedEnum());
	}

    IEnumerator BombExplodedEnum() {
        particleSmoke.Play();
        inputDevice.Vibrate(1f);
        yield return new WaitForSeconds(1f);
        inputDevice.Vibrate(0.3f);
        yield return new WaitForSeconds(0.5f);
        inputDevice.StopVibration();
    }

    public void ReceiveBomb() {
        hasBomb = true;
        inputDevice.Vibrate(bombVibHaving);
    }

    public void SentBomb() {
        hasBomb = false;
        inputDevice.StopVibration();
    }

    public void Restart() {
        hasBomb = false;
        totalPoints += currentPoints;
        playerUI.SetTotalPoints(totalPoints);
        currentPoints = 0;
        particleSmoke.Stop();
    }
}
