﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class PassTheBombPlayer : MonoBehaviour {

    public bool hasBomb = false;
    public float totalPoints = 0;
    public float currentPoints = 0;
    public PassTheBombPlayerUI playerUI;

    [SerializeField] GameObject bomb;
    InputDevice inputDevice;
    PassTheBomb controller;
    int playerIndex;

	float bombVibHaving = 0.1f;
	[Range(0,1f)]
	public float bombVibExplode = 0f;

    void Start() {
        inputDevice = GetComponent<PlayerController>().Device;
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<PassTheBomb>();
        playerUI.SetPlayername(GetComponent<PlayerController>().playername);

		inputDevice.SetLightColor (Color.red);

	}

    void Update() {
        bomb.SetActive(hasBomb);

        if (hasBomb) {
			if (controller.isBombInPlay) {
				inputDevice.Vibrate (bombVibHaving);
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
				inputDevice.StopVibration ();
            }
        }
    }

	public void BombExploded() {
		inputDevice.Vibrate (bombVibExplode);
	}

    public void ReceiveBomb() {
        hasBomb = true;
    }

    public void SentBomb() {
        hasBomb = false;
    }

    public void Restart() {
        hasBomb = false;
        totalPoints += currentPoints;
        playerUI.SetTotalPoints(totalPoints);
        currentPoints = 0;
    }
}
