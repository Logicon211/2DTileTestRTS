using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour {

	[HideInInspector]
	public bool jump = false;				// Condition for whether the player should jump.

	public bool facingRight = true;
	public float speed = 10f;
	public float jumpSpeed = 20f;

	public int maxJumpHeight = 6;


	public int width = 4;
	public int height = 6;

	private Rigidbody2D RB;
	private Animator anim;

	private Transform groundCheck1;
	private Transform groundCheck2;
	private bool grounded = false;			// Whether or not the player is grounded.

	//Bot code. May move out of here?
	public enum BotState
	{
		None = 0,
		MoveTo,
	}

	private BotState mCurrentBotState;
	private bool[] mInputs;
	private int mCurrentNodeId;

	public enum KeyInput
	{
		GoLeft = 0,
		GoRight,
		GoDown,
		Jump,
		Count
	}

	private Level mLevel;
	private Bounds mAABB;
	private List<Vector2i> mPath;
	private int mFramesOfJumping;

	// Use this for initialization
	void Start () {
		mLevel = Level.getLevel ();
		RB = GetComponent<Rigidbody2D>();
		mAABB = GetComponent<SpriteRenderer> ().bounds;
		groundCheck1 = transform.FindChild ("groundCheck1");
		groundCheck2 = transform.FindChild ("groundCheck2");
		mCurrentBotState = BotState.None;
		mFramesOfJumping = 0;
	}

	void Update() {
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		bool grounded1 = Physics2D.Linecast(transform.position, groundCheck1.position, 1 << LayerMask.NameToLayer("Ground")); 
		bool grounded2 = Physics2D.Linecast(transform.position, groundCheck2.position, 1 << LayerMask.NameToLayer("Ground"));
		//Check in between the 2 ground checks
		bool grounded3 = Physics2D.Linecast(groundCheck1.position, groundCheck2.position, 1 << LayerMask.NameToLayer("Ground"));
		grounded = grounded1 || grounded2 || grounded3;
		
		// If the jump button is pressed and the player is grounded then the player should jump.
		if(Input.GetAxis("Vertical") > 0 && grounded) {
			jump = true;
		}

		//Bot movement code
		Vector2 prevDest, currentDest, nextDest;
		bool destOnGround, reachedY, reachedX;
		GetContext(out prevDest, out currentDest, out nextDest, out destOnGround, out reachedX, out reachedY);

		Vector2 pathPosition = mAABB.center - (mAABB.size / 2) + Vector2.one * mLevel.cTileSize * 0.5f;

		mInputs[(int)KeyInput.GoRight] = false;
		mInputs[(int)KeyInput.GoLeft] = false;
		mInputs[(int)KeyInput.Jump] = false;
		mInputs[(int)KeyInput.GoDown] = false;

		if (pathPosition.y - currentDest.y > Constants.cBotMaxPositionError/* && mOnOneWayPlatform TODO: Handle one way platforms*/)
			mInputs[(int)KeyInput.GoDown] = true;

		if (mFramesOfJumping > 0 &&	(!grounded || (reachedX && !destOnGround) || (grounded && destOnGround)))
		{
			mInputs[(int)KeyInput.Jump] = true;
			if (!grounded)
				--mFramesOfJumping;
		}

		if (reachedX && reachedY) {
			mCurrentNodeId++;
			if (mCurrentNodeId >= mPath.Count)
			{
				mCurrentNodeId = -1;
				ChangeState(BotState.None);
				break;
			}
		}
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		anim = GetComponent<Animator>();
		float move = Input.GetAxis("Horizontal");

		if(move != 0/*Input.GetKey(KeyCode.RightArrow*/) {
			if(move > 0) {
				if(!facingRight) {
					Flip();
				}
			} else {
				if(facingRight) {
					Flip();
				}
			}
			RB.velocity = new Vector2(speed * move, RB.velocity.y);
			anim.SetBool("Moving", true);
		} else {
			anim.SetBool("Moving", false);
		}

		if (jump) {
			//RB.AddForce(new Vector2(0f, jumpForce));
			RB.velocity = new Vector2(RB.velocity.x, jumpSpeed);
			anim.SetBool("Jumping", true);
			jump = false; //reset the jump flag so it doesn't happen again immediately
		}
	}

	void OnCollisionEnter2D (Collision2D col) 
	{
		if(col.gameObject.layer == LayerMask.NameToLayer("Ground")) {
			anim.SetBool("Jumping", false);
		}
	}

	void Flip() {
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;

		foreach (Transform child in transform) {
			Vector3 childScale = child.localScale;
			childScale.x *= -1;
			child.localScale = childScale;
		}
	}

	//******Bot Functions below******
	void BotUpdate()
	{
		switch (mCurrentBotState)
		{
		case BotState.None:
			/* no need to do anything */
			break;

		case BotState.MoveTo:
			/* bot movement update logic */
			break;
		}

		CharacterUpdate();
	}

	public void ChangeState(BotState newState)
	{
		mCurrentBotState = newState;
	}

	int GetJumpFrameCount(int deltaY)
	{
		if (deltaY <= 0)
			return 0;
		else
		{
			switch (deltaY)
			{
			case 1:
				return 1;
			case 2:
				return 2;
			case 3:
				return 5;
			case 4:
				return 8;
			case 5:
				return 14;
			case 6:
				return 21;
			default:
				return 30;
			}
		}
	}

	//Probably don't need this as I've already figured this one out sort of
	public void TappedOnTile(Vector2i mapPos)
	{
		while (!(mLevel.IsGround(mapPos.x, mapPos.y)))
			--mapPos.y;

		MoveTo(new Vector2i(mapPos.x, mapPos.y + 1));
	}

	public void MoveTo(Vector2i destination)
	{
		Vector2i startTile = mLevel.GetMapTileAtPoint(mAABB.center - mAABB.size/2 + Vector2.one * mLevel.cTileSize * 0.5f);

		if (grounded && !IsOnGroundAndFitsPos(startTile))
		{
			if (IsOnGroundAndFitsPos(new Vector2i(startTile.x + 1, startTile.y)))
				startTile.x += 1;
			else
				startTile.x -= 1;
			
		}

		var path =  mLevel.mPathFinder.FindPath(
			startTile, 
			destination,
			Mathf.CeilToInt((mAABB.size.x/2)/ 8.0f), 
			Mathf.CeilToInt((mAABB.size.y/2) / 8.0f), 
			(short)maxJumpHeight);

		if (path != null && path.Count > 1) {
			for (var i = path.Count - 1; i >= 0; --i)
				mPath.Add(path[i]);
		}

		if (path != null && path.Count > 1)
		{
			for (var i = path.Count - 1; i >= 0; --i)
				mPath.Add(path[i]);

			mCurrentNodeId = 1;
			ChangeState(BotState.MoveTo);
		}
	}

	bool IsOnGroundAndFitsPos(Vector2i pos)
	{
		for (int y = pos.y; y < pos.y + height; ++y)
		{
			for (int x = pos.x; x < pos.x + width; ++x)
			{
				if (mLevel.IsObstacle(x, y))
					return false;
			}
		}

		for (int x = pos.x; x < pos.x + width; ++x)
		{
			if (mLevel.IsGround(x, pos.y - 1))
				return true;
		}

		return false;
	}

	public void GetContext(out Vector2 prevDest, out Vector2 currentDest, out Vector2 nextDest, out bool destOnGround, out bool reachedX, out bool reachedY)
	{
		//Translate from map coordinates to world coordinates
		prevDest = new Vector2(mPath[mCurrentNodeId - 1].x * mLevel.cTileSize + mLevel.transform.position.x,
			mPath[mCurrentNodeId - 1].y * mLevel.cTileSize + mLevel.transform.position.y);

		currentDest = new Vector2(mPath[mCurrentNodeId].x * mLevel.cTileSize + mLevel.transform.position.x,
			mPath[mCurrentNodeId].y * mLevel.cTileSize + mLevel.transform.position.y);

		nextDest = currentDest;

		if (mPath.Count > mCurrentNodeId + 1)
		{
			nextDest = new Vector2(mPath[mCurrentNodeId + 1].x * mLevel.cTileSize + mLevel.transform.position.x,
				mPath[mCurrentNodeId + 1].y * mLevel.cTileSize + mLevel.transform.position.y);
		}

		destOnGround = false;

		for (int x = mPath[mCurrentNodeId].x; x < mPath[mCurrentNodeId].x + width; ++x)
		{
			if (mLevel.IsGround(x, mPath[mCurrentNodeId].y - 1))
			{
				destOnGround = true;
				break;
			}
		}

		Vector2 pathPosition = mAABB.center - mAABB.size/2 + Vector2.one * mLevel.cTileSize * 0.5f;

		reachedX = (prevDest.x <= currentDest.x && pathPosition.x >= currentDest.x)
			|| (prevDest.x >= currentDest.x && pathPosition.x <= currentDest.x);


		if (reachedX && Mathf.Abs(pathPosition.x - currentDest.x) > Constants.cBotMaxPositionError && Mathf.Abs(pathPosition.x - currentDest.x) < Constants.cBotMaxPositionError*3.0f && !mPrevInputs[(int)KeyInput.GoRight] && !mPrevInputs[(int)KeyInput.GoLeft])
		{
			pathPosition.x = currentDest.x;
			transform.position.x = pathPosition.x - mLevel.cTileSize * 0.5f + (mAABB.size.x/2)/* + mAABBOffset.x Not sure what mAABOfsset is supposed to be*/;
		}

		reachedY = (prevDest.y <= currentDest.y && pathPosition.y >= currentDest.y)
			|| (prevDest.y >= currentDest.y && pathPosition.y <= currentDest.y)
			|| (Mathf.Abs(pathPosition.y - currentDest.y) <= Constants.cBotMaxPositionError);

		if (destOnGround && !grounded)
			reachedY = false;
	}

	public int GetJumpFramesForNode(int prevNodeId)
	{
		int currentNodeId = prevNodeId + 1;

		if (mPath[currentNodeId].y - mPath[prevNodeId].y > 0 && grounded)
		{
			int jumpHeight = 1;
			for (int i = currentNodeId; i < mPath.Count; ++i)
			{
				if (mPath[i].y - mPath[prevNodeId].y >= jumpHeight)
					jumpHeight = mPath[i].y - mPath[prevNodeId].y;
				if (mPath[i].y - mPath[prevNodeId].y < jumpHeight || !mLevel.IsGround(mPath[i].x, mPath[i].y - 1))
					return GetJumpFrameCount(jumpHeight);
			}
		}

		return 0;
	}
}
