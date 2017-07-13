//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Algorithms
{
	#region Enum
    public enum HeuristicFormula
    {
        Manhattan           = 1,
        MaxDXDY             = 2,
        DiagonalShortCut    = 3,
        Euclidean           = 4,
        EuclideanNoSQR      = 5,
        Custom1             = 6
    }
    #endregion
	
    public struct Location
    {
        public Location(int xy, int z)
        {
            this.xy = xy;
            this.z = z;
        }

        public int xy;
        public int z;
    }

    public class PathFinderFast
    {
        
		#region Structs
        internal struct PathFinderNodeFast
        {
            #region Variables Declaration
            public int     F; // f = gone + heuristic
			public int     G; // Gscore is total cost of getting from the start node to the Here (I think?) Partially known, partially hueristic
            public ushort  PX; // Parent
            public ushort  PY;
            public byte    Status;
            public byte    PZ;
			public short   JumpLength;
            #endregion

            public PathFinderNodeFast UpdateStatus(byte newStatus)
            {
                PathFinderNodeFast newNode = this;
                newNode.Status = newStatus;
                return newNode;
            }
        }
		
        #endregion
		
		private List<PathFinderNodeFast>[] nodes;
        private Stack<int> touchedLocations;
		
        #region Variables Declaration
        // Heap variables are initializated to default, but I like to do it anyway
		private MapTile[,]                      mGrid                   = null;
        private PriorityQueueB<Location>        mOpen                   = null;
        private List<Vector2i>                  mClose                  = null;
        private bool                            mStop                   = false;
        private bool                            mStopped                = true;
        private HeuristicFormula                mFormula                = HeuristicFormula.Manhattan;
        private bool                            mDiagonals              = true;
        private int                             mHEstimate              = 2;
        private bool                            mPunishChangeDirection  = false;
        private bool                            mTieBreaker             = false;
        private bool                            mHeavyDiagonals         = false;
        private int                             mSearchLimit            = 2000;
        private double                          mCompletedTime          = 0;
        private bool                            mDebugProgress          = false;
        private bool                            mDebugFoundPath         = false;
        private byte                            mOpenNodeValue          = 1;
        private byte                            mCloseNodeValue         = 2;
        
        //Promoted local variables to member variables to avoid recreation between calls
        private int                             mH                      = 0;
        private Location                        mLocation               ;
        private int                             mNewLocation            = 0;
        private PathFinderNodeFast mNode;
        private ushort                          mLocationX              = 0;
        private ushort                          mLocationY              = 0;
        private ushort                          mNewLocationX           = 0;
        private ushort                          mNewLocationY           = 0;
        private int                             mCloseNodeCounter       = 0;
        private ushort                          mGridX                  = 0;
        private ushort                          mGridY                  = 0;
        private ushort                          mGridXMinus1            = 0;
        private ushort                          mGridXLog2              = 0;
        private bool                            mFound                  = false;
        private sbyte[,]                        mDirection              = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
        private int                             mEndLocation            = 0;
        private int                             mNewG                   = 0;
		
		public Level mLevel;

		private int numberOfSpaceCorrection = 5;
        #endregion
		
        #region Constructors
        public PathFinderFast(Level level)
        {
			if (level == null)
				throw new Exception("Map cannot be null");
			if (level.mapTiles == null)
                throw new Exception("Grid cannot be null");
			
			mLevel = level;
			mGrid           = level.mapTiles;
            mGridX          = (ushort) (mGrid.GetUpperBound(0) + 1);
            mGridY          = (ushort) (mGrid.GetUpperBound(1) + 1);
            mGridXMinus1    = (ushort) (mGridX - 1);
            mGridXLog2      = (ushort) Math.Log(mGridX, 2);

            if (Math.Log(mGridX, 2) != (int) Math.Log(mGridX, 2) ||
                Math.Log(mGridY, 2) != (int) Math.Log(mGridY, 2))
                throw new Exception("Invalid Grid, size in X and Y must be power of 2");

            if (nodes == null || nodes.Length != (mGridX * mGridY))
            {
                nodes = new List<PathFinderNodeFast>[mGridX * mGridY];
                touchedLocations = new Stack<int>(mGridX * mGridY);
                mClose = new List<Vector2i>(mGridX * mGridY);
            }

            for (var i = 0; i < nodes.Length; ++i)
                nodes[i] = new List<PathFinderNodeFast>(1);

            mOpen = new PriorityQueueB<Location>(new ComparePFNodeMatrix(nodes));
        }
        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public HeuristicFormula Formula
        {
            get { return mFormula; }
            set { mFormula = value; }
        }

        public bool Diagonals
        {
            get { return mDiagonals; }
            set 
            { 
                mDiagonals = value; 
                if (mDiagonals)
                    mDirection = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
                else
                    mDirection = new sbyte[4,2]{{0,-1} , {1,0}, {0,1}, {-1,0}};
            }
        }

        public bool HeavyDiagonals
        {
            get { return mHeavyDiagonals; }
            set { mHeavyDiagonals = value; }
        }

        public int HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public bool PunishChangeDirection
        {
            get { return mPunishChangeDirection; }
            set { mPunishChangeDirection = value; }
        }

        public bool TieBreaker
        {
            get { return mTieBreaker; }
            set { mTieBreaker = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public double CompletedTime
        {
            get { return mCompletedTime; }
            set { mCompletedTime = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }
        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

		public Vector2i FindFirstOpenTileBelow(Vector2i start) {
			if(start.x >= mLevel.mWidth || start.x < 0 || start.y >= mLevel.mHeight || start.y < 0) {
				return null;
			}
			int y = start.y;
			while (y - 1 > 0) {
				if (mLevel.IsGround(start.x, y)) {
					return new Vector2i (start.x, y);
				}
				else {
					y--;
				}
			}
			return null;
		}

		public Vector2i FindAlternativeEndPoint (Vector2i start, Vector2i end) {
			if (start.y > end.y) {
				end.y = end.y + numberOfSpaceCorrection;
			} else if (start.y < end.y) {
				end.y = end.y - numberOfSpaceCorrection;
			}

			if (start.x > end.x) {
				end.x = end.x + numberOfSpaceCorrection;
			} else if (start.x < end.x) {
				end.x = end.x -  numberOfSpaceCorrection;
			}

			if (end.x < 0 || end.y < 0 || end.x >= mLevel.mWidth || end.y >= mLevel.mHeight) {
				return null;
			}

			return end;
		}

		public List<Vector2i> FindPath(Vector2i start, Vector2i end, int characterWidth, int characterHeight, short maxCharacterJumpHeight, int numberOfTries)
        {
            lock(this)
            {
                while (touchedLocations.Count > 0)
                    nodes[touchedLocations.Pop()].Clear();
				
				var inSolidTile = true;

				//This starts at the bottom left and checks rightward for intervening ground nodes. If it's empty we're good. If it's not we move to the left a bit to try again
//				for (var i = 0; i < characterWidth; ++i)
//				{
//					inSolidTile = false;
//					for (var w = 0; w < characterWidth; ++w)
//					{
//						if ((end.x + w < 0 || end.x + w >= mLevel.mWidth) || mGrid[end.x + w, end.y] != null || mGrid[end.x + w, end.y + characterHeight - 1] != null)
//						{
//							inSolidTile = true;
//							break;
//						}
//
//					}
//					if (inSolidTile == false)
//					{
//						for (var h = 1; h < characterHeight - 1; ++h)
//						{
//							if ((end.y + h < 0 || end.y + h >= mLevel.mHeight) || mGrid[end.x, end.y + h] != null || mGrid[end.x + characterWidth - 1, end.y + h] != null)
//							{
//								inSolidTile = true;
//								break;
//							}
//						}
//					}
//
//					//TODO: end.x -= characterWidth - 1 is going waaay to far to the left in order to get a valid end spot I think
//					if (inSolidTile)
//						end.x -= /*characterWidth -*/ 1;
//					else
//						break;
//				}

				//Checking if the Endpoint is too small to fit the character
				//Trying to Check from character center outward (shifting left, then right 1 tile at a time
				//If that fails then keep moving the endpoint up until you have a valid endpoint, or you hit the top of the map (Should probably limit this to a certain number of tries)
				int halfWidth = Mathf.FloorToInt(characterWidth/2);
				int origX = end.x;
				while(inSolidTile || end.y >= mLevel.mHeight - characterHeight) {
					for (var i = 1; i < characterWidth+1; ++i)
					{
						inSolidTile = false;
						for (var w = 0; w <= halfWidth; ++w)
						{
							if ((end.x + w < 0 || end.x + w >= mLevel.mWidth) || mGrid[end.x + w, end.y] != null || mGrid[end.x + w, end.y + characterHeight - 1] != null)
							{
								inSolidTile = true;
								break;
							}
							if ((end.x - w < 0 || end.x - w >= mLevel.mWidth) || mGrid[end.x - w, end.y] != null || mGrid[end.x - w, end.y + characterHeight - 1] != null)
							{
								inSolidTile = true;
								break;
							}

						}
						if (inSolidTile == false)
						{
							for (var h = 1; h < characterHeight - 1; ++h)
							{
								if ((end.y + h < 0 || end.y + h >= mLevel.mHeight) || mGrid[end.x - halfWidth, end.y + h] != null || mGrid[end.x + halfWidth, end.y + h] != null)
								{
									inSolidTile = true;
									break;
								}
							}
						}

						if (inSolidTile) {
							if (i % 2 == 0) {
								end.x = origX + (i / 2);
							} else {
								float iHalf = i / 2f;
								end.x = origX - Mathf.CeilToInt (iHalf);
							}
						} else {
							break;
						}
					}

					//If we're still in a space too small. Move the endpoint up one and check there (Possibly extremely inefficient
					if (inSolidTile) {
						end.y++;
					}
				}

				//No actual space (I don't think it will ever hit this since it should keep checking upwards until it hits the top)
				if (inSolidTile == true)
					return null;

                mFound              = false;
                mStop               = false;
                mStopped            = false;
                mCloseNodeCounter   = 0;
                mOpenNodeValue      += 2;
                mCloseNodeValue     += 2;
                mOpen.Clear();

                mLocation.xy = (start.y << mGridXLog2) + start.x;
                mLocation.z = 0;
                mEndLocation                   = (end.y << mGridXLog2) + end.x;

                PathFinderNodeFast firstNode = new PathFinderNodeFast();
                firstNode.G = 0;
                firstNode.F = mHEstimate;
                firstNode.PX = (ushort)start.x;
                firstNode.PY = (ushort)start.y;
                firstNode.PZ = 0;
                firstNode.Status = mOpenNodeValue;

				bool startsOnGround = false;

				//TODO: Check character width from bottom center instead of bottom left
				for (int x = start.x; x < start.x + characterWidth; ++x)
				{
					if (mLevel.IsGround(x, start.y - 1))
					{
						startsOnGround = true;
						break;
					}
				}

				if (startsOnGround)
					firstNode.JumpLength = 0;
				else
					firstNode.JumpLength = (short)(maxCharacterJumpHeight * 2);
				
				nodes[mLocation.xy].Add(firstNode);
                touchedLocations.Push(mLocation.xy);

                mOpen.Push(mLocation);
				
                while(mOpen.Count > 0 && !mStop)
                {
                    mLocation    = mOpen.Pop();

                    //Is it in closed list? means this node was already processed
                    if (nodes[mLocation.xy][mLocation.z].Status == mCloseNodeValue)
                        continue;

                    mLocationX = (ushort) (mLocation.xy & mGridXMinus1);
                    mLocationY = (ushort)(mLocation.xy >> mGridXLog2);

                    if (mLocation.xy == mEndLocation)
                    {
                        nodes[mLocation.xy][mLocation.z] = nodes[mLocation.xy][mLocation.z].UpdateStatus(mCloseNodeValue);
                        mFound = true;
                        break;
                    }

					//No path found
                    if (mCloseNodeCounter > mSearchLimit)
                    {
						end = FindAlternativeEndPoint (start, end);
						if (end == null) {
							mStopped = true;
							return null;
						}

						end = FindFirstOpenTileBelow (end);
						if (end == null) {
							mStopped = true;
							return null;
						}

						if (numberOfTries >= 50) {
							mStopped = true;
							return null;
						} else {
							return FindPath(start, end, characterWidth, characterHeight, maxCharacterJumpHeight, numberOfTries+1);
						}
                    }

                    //Lets calculate each successors
                    for (var i=0; i<(mDiagonals ? 8 : 4); i++)
                    {
                        mNewLocationX = (ushort) (mLocationX + mDirection[i,0]);
                        mNewLocationY = (ushort) (mLocationY + mDirection[i,1]);
                        mNewLocation  = (mNewLocationY << mGridXLog2) + mNewLocationX;

						//Cut children loop early if child is out of bounds of maptile array
						if (mNewLocationX < 0 || mNewLocationX >= mLevel.mWidth || mNewLocationY < 0 || mNewLocationY >= mLevel.mHeight) {
							goto CHILDREN_LOOP_END;
						}

						var onGround = false;
						var atCeiling = false;

						//TODO: Check character with from bottom center instead of bottom left
//						for (var w = 0; w < characterWidth; ++w)
//						{
//							if (mGrid[mNewLocationX + w, mNewLocationY] != null
//								|| mGrid[mNewLocationX + w, mNewLocationY + characterHeight - 1] != null)
//								goto CHILDREN_LOOP_END;
//
//							if (mLevel.IsGround(mNewLocationX + w, mNewLocationY - 1))
//								onGround = true;
//							else if (mGrid[mNewLocationX + w, mNewLocationY + characterHeight] != null)
//								atCeiling = true;
//						}
//						for (var h = 1; h < characterHeight - 1; ++h)
//						{
//							if (mGrid[mNewLocationX, mNewLocationY + h] != null
//								|| mGrid[mNewLocationX + characterWidth - 1, mNewLocationY + h] != null)
//								goto CHILDREN_LOOP_END;
//						}

						//Trying to check from center out
						try {
							//int halfWidth = Mathf.FloorToInt(characterWidth/2);
							for (var w = 0; w <= halfWidth; ++w)
							{
								if (mGrid[mNewLocationX + w, mNewLocationY] != null
									|| mGrid[mNewLocationX + w, mNewLocationY + characterHeight - 1] != null)
									goto CHILDREN_LOOP_END;

								if (mLevel.IsGround(mNewLocationX + w, mNewLocationY - 1))
									onGround = true;
								else if (mGrid[mNewLocationX + w, mNewLocationY + characterHeight] != null)
									atCeiling = true;

								//Checking Other side of character center
								if (mGrid[mNewLocationX - w, mNewLocationY] != null
									|| mGrid[mNewLocationX - w, mNewLocationY + characterHeight - 1] != null)
									goto CHILDREN_LOOP_END;

								if (mLevel.IsGround(mNewLocationX - w, mNewLocationY - 1))
									onGround = true;
								else if (mGrid[mNewLocationX - w, mNewLocationY + characterHeight] != null)
									atCeiling = true;

							}
							for (var h = 1; h < characterHeight - 1; ++h)
							{
									if (mGrid[mNewLocationX - halfWidth, mNewLocationY + h] != null
										|| mGrid[mNewLocationX + halfWidth/* characterWidth - 1*/, mNewLocationY + h] != null)
										goto CHILDREN_LOOP_END;
								
							}
						} catch (IndexOutOfRangeException e) {
							//Index out of range exception just means we're checking past the map bounds. Ignore the proposed node
							goto CHILDREN_LOOP_END;
						}

						//calculate a proper jumplength value for the successor

                        var jumpLength = nodes[mLocation.xy][mLocation.z].JumpLength;
                        short newJumpLength = jumpLength;

						if (atCeiling)
                        {
                            if (mNewLocationX != mLocationX)
                                newJumpLength = (short)Mathf.Max(maxCharacterJumpHeight * 2 + 1, jumpLength + 1);
                            else
                                newJumpLength = (short)Mathf.Max(maxCharacterJumpHeight * 2, jumpLength + 2);
                        }
                        else if (onGround)
							newJumpLength = 0;
						else if (mNewLocationY > mLocationY)
						{
                            if (jumpLength < 2) //first jump is always two block up instead of one up and optionally one to either right or left
                                newJumpLength = 3;
                            else  if (jumpLength % 2 == 0)
                                newJumpLength = (short)(jumpLength + 2);
                            else
                                newJumpLength = (short)(jumpLength + 1);
						}
						else if (mNewLocationY < mLocationY)
						{
							if (jumpLength % 2 == 0)
								newJumpLength = (short)Mathf.Max(maxCharacterJumpHeight * 2, jumpLength + 2);
							else
								newJumpLength = (short)Mathf.Max(maxCharacterJumpHeight * 2, jumpLength + 1);
						}
						else if (!onGround && mNewLocationX != mLocationX)
							newJumpLength = (short)(jumpLength + 1);
						
						if (jumpLength >= 0 && jumpLength % 2 != 0 && mLocationX != mNewLocationX)
							continue;
						
						//if we're falling and succeor's height is bigger than ours, skip that successor
						if (jumpLength >= maxCharacterJumpHeight * 2 && mNewLocationY > mLocationY)
							continue;

                        if (newJumpLength >= maxCharacterJumpHeight * 2 + 6 && mNewLocationX != mLocationX && (newJumpLength - (maxCharacterJumpHeight * 2 + 6)) % 8 != 3)
							continue;


						//The commented out part here would normally be "1" for empty space, but in this case it would be checking a null object so we'll go with this for now
						//If Jumping is too prevalent (Or not enough) change the amount we divide the newJumpLength by here. 4 is the default from the algorithm but it sometimes makes the path pretty jumpy
                        mNewG = nodes[mLocation.xy][mLocation.z].G + /*mGrid[mNewLocationX, mNewLocationY]*/1 + newJumpLength / 4;

                        if (nodes[mNewLocation].Count > 0)
                        {
                            int lowestJump = short.MaxValue;
                            bool couldMoveSideways = false;
                            for (int j = 0; j < nodes[mNewLocation].Count; ++j)
                            {
                                if (nodes[mNewLocation][j].JumpLength < lowestJump)
                                    lowestJump = nodes[mNewLocation][j].JumpLength;

                                if (nodes[mNewLocation][j].JumpLength % 2 == 0 && nodes[mNewLocation][j].JumpLength < maxCharacterJumpHeight * 2 + 6)
                                    couldMoveSideways = true;
                            }

                            if (lowestJump <= newJumpLength && (newJumpLength % 2 != 0 || newJumpLength >= maxCharacterJumpHeight * 2 + 6 || couldMoveSideways))
                                continue;
                        }
						
                        switch(mFormula)
                        {
                            default:
                            case HeuristicFormula.Manhattan:
                                mH = mHEstimate * (Mathf.Abs(mNewLocationX - end.x) + Mathf.Abs(mNewLocationY - end.y));
                                break;
                            case HeuristicFormula.MaxDXDY:
                                mH = mHEstimate * (Math.Max(Math.Abs(mNewLocationX - end.x), Math.Abs(mNewLocationY - end.y)));
                                break;
                            case HeuristicFormula.DiagonalShortCut:
                                var h_diagonal  = Math.Min(Math.Abs(mNewLocationX - end.x), Math.Abs(mNewLocationY - end.y));
                                var h_straight  = (Math.Abs(mNewLocationX - end.x) + Math.Abs(mNewLocationY - end.y));
                                mH = (mHEstimate * 2) * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                                break;
                            case HeuristicFormula.Euclidean:
                                mH = (int) (mHEstimate * Math.Sqrt(Math.Pow((mNewLocationY - end.x) , 2) + Math.Pow((mNewLocationY - end.y), 2)));
                                break;
                            case HeuristicFormula.EuclideanNoSQR:
                                mH = (int) (mHEstimate * (Math.Pow((mNewLocationX - end.x) , 2) + Math.Pow((mNewLocationY - end.y), 2)));
                                break;
                            case HeuristicFormula.Custom1:
                                var dxy       = new Vector2i(Math.Abs(end.x - mNewLocationX), Math.Abs(end.y - mNewLocationY));
                                var Orthogonal  = Math.Abs(dxy.x - dxy.y);
                                var Diagonal    = Math.Abs(((dxy.x + dxy.y) - Orthogonal) / 2);
                                mH = mHEstimate * (Diagonal + Orthogonal + dxy.x + dxy.y);
                                break;
                        }

                        PathFinderNodeFast newNode = new PathFinderNodeFast();
                        newNode.JumpLength = newJumpLength;
                        newNode.PX = mLocationX;
                        newNode.PY = mLocationY;
                        newNode.PZ = (byte)mLocation.z;
                        newNode.G = mNewG;
                        newNode.F = mNewG + mH;
                        newNode.Status = mOpenNodeValue;

                        if (nodes[mNewLocation].Count == 0)
                            touchedLocations.Push(mNewLocation);

                        nodes[mNewLocation].Add(newNode);
                        mOpen.Push(new Location(mNewLocation, nodes[mNewLocation].Count - 1));
						
					CHILDREN_LOOP_END:
						continue;
                    }

                    nodes[mLocation.xy][mLocation.z] = nodes[mLocation.xy][mLocation.z].UpdateStatus(mCloseNodeValue);
                    mCloseNodeCounter++;
                }

                if (mFound)
                {
                    mClose.Clear();
                    var posX = end.x;
                    var posY = end.y;

                    var fPrevNodeTmp = new PathFinderNodeFast();
                    var fNodeTmp = nodes[mEndLocation][0];

                    var fNode = end;
                    var fPrevNode = end;

                    var loc = (fNodeTmp.PY << mGridXLog2) + fNodeTmp.PX;

                    while (fNode.x != fNodeTmp.PX || fNode.y != fNodeTmp.PY)
                    {
                        var fNextNodeTmp = nodes[loc][fNodeTmp.PZ];

                        if ((mClose.Count == 0)
                            || (mLevel.IsOneWayPlatform(fNode.x, fNode.y - 1))
                            || (mGrid[fNode.x, fNode.y - 1] != null && mLevel.IsOneWayPlatform(fPrevNode.x, fPrevNode.y - 1))
                            || (fNodeTmp.JumpLength == 3)
                            || (fNextNodeTmp.JumpLength != 0 && fNodeTmp.JumpLength == 0)                                                                                                       //mark jumps starts
                            || (fNodeTmp.JumpLength == 0 && fPrevNodeTmp.JumpLength != 0)                                                                                                       //mark landings
                            || (fNode.y > mClose[mClose.Count - 1].y && fNode.y > fNodeTmp.PY)
                            || (fNode.y < mClose[mClose.Count - 1].y && fNode.y < fNodeTmp.PY)
                            || ((mLevel.IsGround(fNode.x - 1, fNode.y) || mLevel.IsGround(fNode.x + 1, fNode.y))
                                && fNode.y != mClose[mClose.Count - 1].y && fNode.x != mClose[mClose.Count - 1].x))
                            mClose.Add(fNode);

                        fPrevNode = fNode;
                        posX = fNodeTmp.PX;
                        posY = fNodeTmp.PY;
                        fPrevNodeTmp = fNodeTmp;
                        fNodeTmp = fNextNodeTmp;
                        loc = (fNodeTmp.PY << mGridXLog2) + fNodeTmp.PX;
                        fNode = new Vector2i(posX, posY);
                    }

                    mClose.Add(fNode);

                    mStopped = true;

					Debug.Log ("Number of Tries for successful path: " + numberOfTries);
                    return mClose;
                }
                mStopped = true;
                return null;
            }
        }
        #endregion

        #region Inner Classes
        internal class ComparePFNodeMatrix : IComparer<Location>
        {
            #region Variables Declaration
            List<PathFinderNodeFast>[] mMatrix;
            #endregion

            #region Constructors
            public ComparePFNodeMatrix(List<PathFinderNodeFast>[] matrix)
            {
                mMatrix = matrix;
            }
            #endregion

            #region IComparer Members
            public int Compare(Location a, Location b)
            {
                if (mMatrix[a.xy][a.z].F > mMatrix[b.xy][b.z].F)
                    return 1;
                else if (mMatrix[a.xy][a.z].F < mMatrix[b.xy][b.z].F)
                    return -1;
                return 0;
            }
            #endregion
        }
        #endregion
    }

}
