﻿using AnimationEnums;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace AnimationEnums {
	public enum IdleOrWalking {
		Idle, Walking
	}
	
	public enum Jumping {
		Jumping, Spinning, Exhale
	}

	public enum Flying {
		Flying, Exhaling
	}

	public enum Inhaling {
		Inhaling, FinishInhaling
	}

	public enum Inhaled {
		Idle, Walking
	}

	public enum InhaledJumping {
		Jumping
	}
}

public class Kirby : CharacterBase {
	public float speed = 6f;
	public float jumpSpeed = 12.5f;
	public float flySpeed = 7f;

	public float knockbackSpeed = 8f;
	public float knockbackTime = 0.2f;
	public bool isExhaling = false;
	public bool inhaleStarted = false;

	public int health = 6;
	public static int livesRemaining = 4;

	public StarProjectile starProjectilePrefab;
	public GameObject slideSmokePrefab;

	private bool isSpinning = false;
	private GameObject inhaleArea;
	private EnemyBase inhaledEnemy;

	bool invulnurable;

	private Animator animator;

	// TODO: This is a bad way of doing this. See KnockbackEnterState
	private GameObject enemyOther;

	public enum State {
		IdleOrWalking, Jumping, Flying, Knockback, Ducking, Sliding, Inhaling, Inhaled, Die,
		InhaledJumping, InhaledKnockback, Shooting, Swallowing, UsingAbility
	}
	
	new public void Start() {
		base.Start();
		GameObject.Find("LivesRemaining").GetComponent<LivesRemaining>().setLivesRemaining(livesRemaining);
		animator = GetComponentInChildren<Animator>();
		CurrentState = State.Jumping;
		dir = Direction.Right;
		inhaleArea = transform.Find("Sprite/InhaleArea").gameObject;
		inhaleArea.SetActive(false);
	}

	private void killEnemy(GameObject other) {
		inhaledEnemy = other.GetComponent<EnemyBase>();
		Destroy(other);
		am.animate((int) Inhaling.FinishInhaling);
	}

	public void enemyCollisionCallback(GameObject other) {
		killEnemy(other);
	}

	private void OnCollideWithEnemy(GameObject enemy) {
		enemyOther = enemy;
		Destroy(enemy);
		TakeDamage();
		CurrentState = (inhaledEnemy == null) ? State.Knockback : State.InhaledKnockback;
	}

	private void CommonOnCollisionEnter2D(Collision2D other) {
		if (other.gameObject.tag == "enemy") {
			OnCollideWithEnemy(other.gameObject);
		}
	}

	private IEnumerator ShowSmoke() {
		int smokeDir = dir == Direction.Right ? -1 : 1;
		GameObject smoke = CreateSmoke(smokeDir * 2);
		yield return new WaitForSeconds(0.2f);
		Destroy(smoke);
	}

	private void HandleHorizontalMovement(ref Vector2 vel) {
		float h = Input.GetAxis("Horizontal");
		if (h > 0 && dir != Direction.Right) {
			StartCoroutine(ShowSmoke());
			Flip();
		} else if (h < 0 && dir != Direction.Left) {
			StartCoroutine(ShowSmoke());
			Flip();
		}
		vel.x = h * speed;
	}
	
	#region IDLE_OR_WALKING

	private void IdleOrWalkingUpdate() {
		Vector2 vel = rigidbody2D.velocity;
		HandleHorizontalMovement(ref vel);
		if (Input.GetKeyDown(KeyCode.X)) {
			vel.y = jumpSpeed;
			CurrentState = State.Jumping;
		} else if (Input.GetKey(KeyCode.Z) && ability == null) {
			CurrentState = State.Inhaling;
		} else if (Input.GetKeyDown(KeyCode.Z) && ability != null) {
			CurrentState = State.UsingAbility;
		} else if (Input.GetKey(KeyCode.UpArrow)) {
			vel.y = flySpeed;
			CurrentState = State.Flying;
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			CurrentState = State.Ducking;
		} else {
			if (vel.x == 0) {
				am.animate((int) IdleOrWalking.Idle);
			} else {
				am.animate((int) IdleOrWalking.Walking);
			}
		}
		rigidbody2D.velocity = vel;
	}

	private void IdleOrWalkingOnCollisionEnter2D(Collision2D other) {
		CommonOnCollisionEnter2D(other);
	}

	#endregion

	#region Inhaled
	
	private void InhaledUpdate() {
		Vector2 vel = rigidbody2D.velocity;
		HandleHorizontalMovement(ref vel);
		if (Input.GetKey (KeyCode.X)) {
			vel.y = jumpSpeed;
			CurrentState = State.InhaledJumping;
		} else if (Input.GetKeyDown(KeyCode.Z)) {
			CurrentState = State.Shooting;
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			CurrentState = State.Swallowing;
		} else {
			if (vel.x == 0) {
				am.animate((int) Inhaled.Idle);
			} else {
				am.animate((int) Inhaled.Walking);
			}
		}
		rigidbody2D.velocity = vel;
	}

	private void InhaledOnCollisionEnter2D(Collision2D other) {
		CommonOnCollisionEnter2D(other);
	}

	#endregion

	#region Swallowing
	
	private IEnumerator SwallowingEnterState() {
		ability = inhaledEnemy.ability;
		yield return new WaitForSeconds (0.5f);
		CurrentState = State.IdleOrWalking;
	}
	
	#endregion

	#region JUMPING

	private void JumpingUpdate() {
		Vector2 vel = rigidbody2D.velocity;
		HandleHorizontalMovement(ref vel);
		if (Input.GetKey(KeyCode.UpArrow)) {
			CurrentState = State.Flying;
		} else {
			if (Input.GetKeyUp(KeyCode.X)) {
				vel.y = Mathf.Min(vel.y, 0);
			} else if (Input.GetKey(KeyCode.Z)) {
				if (ability == null) {
					CurrentState = State.Inhaling;
				} else {
					CurrentState = State.UsingAbility;
				}
			}
			if (!isSpinning && Mathf.Abs(vel.y) < 0.4) {
				isSpinning = true;
				StartCoroutine(SpinAnimation());
			}
		}
		rigidbody2D.velocity = vel;
	}

	private IEnumerator SpinAnimation() {
		am.animate((int) Jumping.Spinning);
		yield return new WaitForSeconds(0.2f);
		am.animate((int) Jumping.Jumping);
		isSpinning = false;
	}

	private void JumpingOnCollisionEnter2D(Collision2D other) {
		CommonOnCollisionEnter2D(other);
	}

	private void JumpingOnCollisionStay2D(Collision2D other) {
		if (other.gameObject.tag == "ground") {
			if (other.contacts.Length > 0 && rigidbody2D.velocity.y <= 0 &&
			    Vector2.Dot(other.contacts[0].normal, Vector2.up) > 0.5) {
				// Collision was on bottom
				CurrentState = State.IdleOrWalking;
			}
		}
	}

	#endregion

	#region Shooting

	private IEnumerator ShootingEnterState() {
		StarProjectile star = Instantiate(starProjectilePrefab) as StarProjectile;
		star.gameObject.transform.position = transform.position + Vector3.up * 0.1f; // Don't touch the ground
		star.direction = (dir == Direction.Right ? 1 : -1);
		inhaledEnemy = null;
		CurrentState = State.Jumping;
		yield break;
	}

	#endregion

	#region Shooting
	
	private IEnumerator UsingAbilityEnterState() {
		StartCoroutine(UseAbility(true));
		StartCoroutine(SlowDown(0.5f));
		yield return null;
	}

	protected override void OnAbilityFinished() {
		CurrentState = State.IdleOrWalking;
	}
	
	#endregion

	#region InhaledJumping
	
	private void InhaledJumpingUpdate() {
		Vector2 vel = rigidbody2D.velocity;
		HandleHorizontalMovement(ref vel);
		if (Input.GetKeyDown(KeyCode.Z)) {
			CurrentState = State.Shooting;
		}
		rigidbody2D.velocity = vel;
	}

	private void InhaledJumpingOnCollisionEnter2D(Collision2D other) {
		CommonOnCollisionEnter2D(other);
	}
	
	private void InhaledJumpingOnCollisionStay2D(Collision2D other) {
		if (other.gameObject.tag == "ground") {
			if (other.contacts.Length > 0 &&
			    Vector2.Dot(other.contacts[0].normal, Vector2.up) > 0.5) {
				// Collision was on bottom
				CurrentState = State.Inhaled;
			}
		}
	}
	
	#endregion

	#region FLYING

	private IEnumerator FlyingEnterState() {
		speed -= 2;
		yield break;
	}

	private IEnumerator FlyingExitState() {
		speed += 2;
		animator.speed = 1f;
		yield break;
	}

	private void FlyingUpdate() {
		Vector2 vel = rigidbody2D.velocity;
		HandleHorizontalMovement(ref vel);
		if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.UpArrow)) {
			vel.y = flySpeed;
			animator.speed = 1f;
		} else {
			vel.y = Mathf.Max(vel.y, -1 * flySpeed * 0.7f);
			AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
			if (info.IsName("FlyingMiddle")) {
				animator.speed = 0.3f;
			}
		}
		if (Input.GetKey(KeyCode.Z)) {
			animator.speed = 1f;
			if (!isExhaling) {
				isExhaling = true;
				StartCoroutine(Exhale());
			}
		}
		rigidbody2D.velocity = vel;
	}

	private IEnumerator Exhale() {
		am.animate((int) Flying.Exhaling);
		yield return new WaitForSeconds(0.4f);
		CurrentState = State.Jumping;
		isExhaling = false;
	}
	
	private void FlyingOnCollisionEnter2D(Collision2D other) {
		CommonOnCollisionEnter2D(other);
	}

	#endregion

	private void TakeDamage() {
		GameObject go = GameObject.Find("HealthBarItem" + health);
		Animator animator = go.GetComponent<Animator>();
		animator.SetBool("Remove", true);
		
		health -= 1;
		if (health == 0) {
			CurrentState = State.Die;
			return;
		}
	}

	#region KNOCKBACK

	private IEnumerator KnockbackEnterState() {
		float xVel = knockbackSpeed * (enemyOther.transform.position.x > transform.position.x ? -1 : 1);
		updateXVelocity(xVel);
		yield return new WaitForSeconds(knockbackTime);
		CurrentState = State.IdleOrWalking;
		rigidbody2D.velocity = Vector2.zero;
	}

	#endregion

	#region KNOCKBACK
	
	private IEnumerator InhaledKnockbackEnterState() {
		float xVel = knockbackSpeed * (enemyOther.transform.position.x > transform.position.x ? -1 : 1);
		updateXVelocity(xVel);
		yield return new WaitForSeconds(knockbackTime);
		CurrentState = State.Inhaled;
		rigidbody2D.velocity = Vector2.zero;
	}
	
	#endregion

	#region Ducking

	public void DuckingUpdate() {
		if (!Input.GetKey(KeyCode.DownArrow)) {
			CurrentState = State.IdleOrWalking;
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			CurrentState = State.Flying;
		}
		if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X)) {
			CurrentState = State.Sliding;
		}
		Vector2 vel = rigidbody2D.velocity;
		vel.x *= 0.9f;
		rigidbody2D.velocity = vel;
	}

	#endregion

	#region Sliding

	GameObject CreateSmoke(int smokeDir) {
		GameObject smoke = Instantiate(slideSmokePrefab) as GameObject;
		smoke.transform.parent = transform;

		Vector3 offset = 0.5f * Vector3.left * smokeDir;
		if (Direction.Left == dir) {
			offset += Vector3.right * 0.5f;
		}
		
		smoke.transform.position = transform.position + offset;

		return smoke;
	}
	public IEnumerator SlidingEnterState() {
		int slideDir = dir == Direction.Right ? 1 : -1;
		GameObject smoke = CreateSmoke(slideDir);

		updateXVelocity(11 * slideDir);
		yield return new WaitForSeconds(0.4f);
		StartCoroutine(SlowDown(0.2f));
		yield return new WaitForSeconds(0.2f);
		CurrentState = State.Ducking;
		Destroy(smoke);
	}

	private void SlidingOnCollisionEnter2D(Collision2D other) {
		killEnemy(other.gameObject);
	}

	#endregion
	
	#region Inhaling

	private IEnumerator InhalingEnterState() {
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("kirby"), LayerMask.NameToLayer("enemy"));
		inhaleArea.SetActive(true);
		StartCoroutine(SlowDown(0.5f));
		yield return new WaitForSeconds(0.5f);
		inhaleStarted = true;
	}

	private IEnumerator InhalingExitState() {
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("kirby"), LayerMask.NameToLayer("enemy"), false);
		inhaleArea.SetActive(false);
		inhaleStarted = false;
		yield break;
	}
		
	public void InhalingUpdate() {
		if (inhaleStarted && !Input.GetKey(KeyCode.Z) && am.SubState != (int) Inhaling.FinishInhaling) {
			CurrentState = State.IdleOrWalking;
		}
	}

	public void OnFinishedInhaling() {
		CurrentState = Kirby.State.Inhaled;
	}

	#endregion

	#region DIE

	private IEnumerator DieEnterState() {
		livesRemaining -= 1;
		if (livesRemaining < 0) {
			Application.Quit(); // TODO
		} else {
			Application.LoadLevel("Main");
		}
		yield break;
	}

	#endregion

	public void TakeHit(GameObject particle) {
		if (invulnurable) {
			return;
		}
		TakeDamage();
		enemyOther = particle;
		CurrentState = (inhaledEnemy == null) ? State.Knockback : State.InhaledKnockback;
		StartCoroutine("Invulnerability");
	}

	public IEnumerator Invulnerability() {
		invulnurable = true;
		yield return new WaitForSeconds (2f);
		invulnurable = false;
	}
}
