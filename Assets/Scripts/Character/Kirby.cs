﻿using UnityEngine;
using System.Collections;

public class Kirby : MonoBehaviour {
	public float speed = 6f;
	public float jumpSpeed = 12.5f;
	public float flySpeed = 7f;

	public float knockBackSpeed = 8f;
	public float knockBackTime = 0.2f;
	public float lastKnockBack = 0f;

	/*
	 * All Vertical/Inhale state combinations are valid except for
	 * Flying/Inhaling and Flying/Inhaled
	 */
	public VerticalState verticalState = VerticalState.JUMPING;
	private InhaleState inhaleState = InhaleState.NOT_INHALING;
	private Animator animator;

	public enum VerticalState {
		GROUND, JUMPING, FLYING, KNOCKBACK
	}
	private enum InhaleState {
		NOT_INHALING, INHALING, INHALED
	}

	void Start() {
		animator = this.GetComponent<Animator>();
	}

	void HandleKnockBack(ref Vector2 vel) {
		if (verticalState == VerticalState.KNOCKBACK) {
			if (Time.time > lastKnockBack + knockBackTime) {
				verticalState = VerticalState.GROUND;
				vel.x = 0;
				vel.y = 0;
			}
		}
	}

	void Update() {
		/*
		 * vel must **only** be modified, never reassigned. C# doesn't have the equivalent of Java's final keyword,
		 * so this cannot be programmatically enforced.
		 */
		Vector2 vel = rigidbody2D.velocity;
		HandleInhaling(ref vel); // This must come before other handlers
		HandleKnockBack(ref vel);
		HandleHorizontalMovement(ref vel);
		HandleJumping(ref vel);
		HandleFlying(ref vel);
		rigidbody2D.velocity = vel;
	}

	void KnockBack(Collision2D other) {
		verticalState = VerticalState.KNOCKBACK;
		float xVel = knockBackSpeed;
		if (other.transform.position.x > transform.position.x) {
			xVel *= -1;
		}
		lastKnockBack = Time.time;
		rigidbody2D.velocity = new Vector2(xVel, 0);
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.tag == "enemy") {
			Destroy (collision.gameObject);
			KnockBack(collision);
		} else {
			verticalState = VerticalState.GROUND;
		}
	}

	void HandleInhaling(ref Vector2 vel) {
		// TODO
	}

	void HandleHorizontalMovement(ref Vector2 vel) {
		if (inhaleState == InhaleState.INHALING ||
		    verticalState == VerticalState.KNOCKBACK) {
			return;
		}

		float h = Input.GetAxis("Horizontal");

		if (h > 0) {
			animator.SetInteger ("Direction", 1);
		} else if (h < 0) {
			animator.SetInteger("Direction", 0);
		}

		vel.x = h * speed;
	}

	void HandleJumping(ref Vector2 vel) {
		if (inhaleState == InhaleState.INHALING
		    || verticalState == VerticalState.KNOCKBACK) {
			return;
		}

		if (Input.GetKey(KeyCode.X)) {
			if (verticalState == VerticalState.GROUND) {
				vel.y = jumpSpeed;
			}
			verticalState = VerticalState.JUMPING;
		}
		if (Input.GetKeyUp(KeyCode.X)) {
			if (verticalState == VerticalState.JUMPING) {
				vel.y = Mathf.Min(vel.y, 0);
			}
		}
	}
	

	void HandleFlying(ref Vector2 vel) {
		if (inhaleState == InhaleState.INHALING ||
		    inhaleState == InhaleState.INHALED ||
		    verticalState == VerticalState.KNOCKBACK) {
			return;
		}

		if (verticalState == VerticalState.FLYING) {
			vel.y = -1 * flySpeed;
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			vel.y = flySpeed;
			verticalState = VerticalState.FLYING;
		}
	}
}
