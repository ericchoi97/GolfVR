﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum ActionState{
	Idle,
	Loading,
	Loaded,
	Firing,
	Fired, 
	Won,
	OutOfBound,
	MoveToTheBall
}

public class MainScript : MonoBehaviour {

	[HideInInspector]
	public ActionState currentAction;
		
	public Terrain CurrentTerrain{
		get{ return GetCurrentHole ().Terrain; }
	}

	/*
	 * 	Public Properties
	 */
	public GameObject CardboardGameObject; 	//TODO script

	public PlayerScript Player;
	public ClubScript Club;
	public BallScript Ball;
	public HolesScript Holes;
	public HudScript Hud;

	private int score;


	// Singleton
	private static MainScript instance;
	public static MainScript Get(){
		return instance;
	}

	/*
	 * Initialisation
	 */
	void Start () {
		instance = this;

		score = 0;
		currentAction = ActionState.Idle;
	}

	/*
	 * 	Action
	 */
	void FixedUpdate () {

		switch (currentAction) 
		{
			case ActionState.Idle:
				//Do nothing
			break;



			case ActionState.Loading:
				if(!Club.IsLoaded())
				{
					Club.Load();
					Hud.SetPowerBarAmount(Club.LoadingAmount());
				}
				else
				{
					currentAction = ActionState.Loaded;
				}
			break;



			case ActionState.Loaded:
				//Wait
			break;



			case ActionState.Firing:
				Hud.SetPowerBarAmount(0);				
				Club.Fire();

				if(!Ball.IsShooted() && Club.HasShooted())							//Shoot now
				{
					Ball.Shoot(Club.LoadingTime * Club.clubForceCoef, Club.clubAngle, Player.transform.eulerAngles.y);
					Hud.UpdateScore(score++);
				}
				else if (Club.IsFired())
				{					
					currentAction = ActionState.Fired;
				}
			break;



			case ActionState.Fired:					
				if(Ball.IsOutOfBound()){
					currentAction = ActionState.OutOfBound;
					break;
				}		   		
				if(Ball.IsStopped())
				{
					Hud.FadeOut();
					currentAction = ActionState.MoveToTheBall;
				}				
			break;

			case ActionState.Won:	
				Ball.StopAndMove(Holes.CurrentHole.BeginPosition.transform.position); //Go to next hole
				Hud.ShowInformation(Localization.Hole);
				Hud.FadeOut();	
				currentAction = ActionState.MoveToTheBall;
			break;

			case ActionState.OutOfBound:	
				Ball.StopAndGetBackToOldPos();
				Hud.UpdateScore(score++);
				Hud.ShowInformation(Localization.OutOfZone);
				Hud.FadeOut();	
				currentAction = ActionState.MoveToTheBall;				
			break;

			case ActionState.MoveToTheBall:	
				if(Hud.IsFadingIn()){
					Player.transform.position = Ball.transform.position;
					Club.Reset();	
					currentAction = ActionState.Idle;
				}
			break;
		}


		/*
		 * 	Rotation system
		 */
		if (currentAction != ActionState.MoveToTheBall) {
			var headRotation = Cardboard.SDK.HeadPose.Orientation.eulerAngles;		// Head rotation
			var horizontalNeckRotation = headRotation.y;					// y rotation of the neck (horizontally)
			var forwardNeckRotation = headRotation.x;						// x rotation of the neck (forward) 
			var neckVector = Cardboard.SDK.HeadPose.Orientation * Vector3.up;		// Neck vector
		
			var forwardRotationThresholdMin = 10; 							// Player look in direction of the ground/ball
			var forwardRotationThresholdMax = 90; 	

			// Player look at the horizon (normal rotation around the ball)
			var lookHorizontally = forwardNeckRotation < forwardRotationThresholdMin || forwardNeckRotation > forwardRotationThresholdMax;
			if (lookHorizontally) {
				Player.transform.eulerAngles = new Vector3 (0, horizontalNeckRotation, 0);
			} else { // Player look at the ground we follow his neck direction (Horizontal projection of the neck vector)
				var direction = new Vector3 (neckVector.x, 0, neckVector.z);
				Player.transform.rotation = Quaternion.LookRotation (direction); // TODO: add a little threshold?
			}

			//Cardboard top/bottom rotation
			var cardBoardVect = CardboardGameObject.transform.eulerAngles;
			CardboardGameObject.transform.eulerAngles = new Vector3 (forwardNeckRotation, cardBoardVect.y, cardBoardVect.z);
			//Debug.Log("NECK: " + Cardboard.SDK.HeadPose.Orientation  * Vector3.up);
		}
	}



	public void SetCurrentClub(GameObject club){
		this.Club = club.GetComponent<ClubScript> ();
		Player.SetCurrentClub (Club.gameObject);
	}

	public GameObject GetCurrentClub(){
		return Player.GetCurrentClub ();
	}


	public HoleScript GetCurrentHole(){
		return Holes.CurrentHole;
	}

	/*
	 * Watching ball (shoot/release)
	 */
	public void LoadShoot(){
		if (currentAction == ActionState.Idle)
			currentAction = ActionState.Loading;
		/*
		 * TODO: Sounds
		 */
	}

	public void ReleaseShoot(){
		if (currentAction == ActionState.Loading || currentAction == ActionState.Loaded)
			currentAction = ActionState.Firing;
		/*
		 * TODO: Sounds
		 */
	}




	/*
	 * 	Events
	 */
	public void EnterHole()
	{
		currentAction = ActionState.Won;
	}

	public void Win(){
		// TODO
	}
}
