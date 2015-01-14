﻿using System;
using System.Collections.Generic;
using System.Linq;
using Henu.Input;
using Henu.Navigation;
using UnityEngine;

namespace Henu.State {

	/*================================================================================================*/
	public class ArcState {

		public delegate void LevelChangeHandler(int pDirection);
		public event LevelChangeHandler OnLevelChange;

		public static float BackGrabThreshold = 0.6f;
		public static float BackReleaseThreshold = 0.3f;

		public bool IsActive { get; private set; }
		public bool IsLeft { get; private set; }
		public Vector3 Center { get; private set; }
		public Quaternion Rotation { get; private set; }
		public float Size { get; private set; }
		public float Strength { get; private set; }
		public float GrabStrength { get; private set; }
		public ArcSegmentState NearestSegment { get; private set; }

		private readonly IInputHandProvider vInputHandProv;
		private readonly NavigationProvider vNavProv;
		private readonly IList<ArcSegmentState> vSegments;
		private bool vIsGrabbing;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArcState(IInputHandProvider pInputHandProv, NavigationProvider pNavProv) {
			vInputHandProv = pInputHandProv;
			vNavProv = pNavProv;
			vSegments = new List<ArcSegmentState>();

			IsLeft = vInputHandProv.IsLeft;

			OnLevelChange = (d => {});
			vNavProv.OnLevelChange += HandleLevelChange;
			HandleLevelChange(0);
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ArcSegmentState[] GetSegments() {
			return vSegments.ToArray();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		internal void UpdateAfterInput() {
			IInputHand inputHand = vInputHandProv.Hand;

			if ( inputHand == null ) {
				IsActive = false;
				Center = Vector3.zero;
				Rotation = Quaternion.identity;
				Strength = 0;
				GrabStrength = 0;
				return;
			}

			var inputPoints = new List<IInputPoint>(new[] {
				vInputHandProv.IndexPoint,
				vInputHandProv.MiddlePoint,
				vInputHandProv.RingPoint,
				vInputHandProv.PinkyPoint
			});

			IsActive = true;
			Center = inputHand.Center;
			Size = 0;
			Rotation = inputHand.Rotation;

			foreach ( IInputPoint inputPoint in inputPoints ) {
				if ( inputPoint == null ) {
					continue;
				}

				Rotation = Quaternion.Slerp(Rotation, inputPoint.Rotation, 0.1f);
				Size = Math.Max(Size, (inputPoint.Position-Center).sqrMagnitude);
			}

			Size = (float)Math.Sqrt(Size);
			Strength = Math.Max(0, (inputHand.PalmTowardEyes-0.7f)/0.3f);
			GrabStrength = Math.Min(1, inputHand.GrabStrength/BackGrabThreshold);
			CheckGrabGesture(inputHand);
		}

		/*--------------------------------------------------------------------------------------------*/
		internal void UpdateWithCursor(CursorState pCursor) {
			bool allowSelect = (pCursor != null && Strength > 0);
			NearestSegment = null;

			foreach ( ArcSegmentState seg in vSegments ) {
				seg.UpdateWithCursor(pCursor != null ? pCursor.Position : null);

				if ( !allowSelect || seg.HighlightProgress < 1 ) {
					continue;
				}

				if ( NearestSegment == null ) {
					NearestSegment = seg;
					continue;
				}

				if ( seg.HighlightDistance < NearestSegment.HighlightDistance ) {
					NearestSegment = seg;
				}
			}

			foreach ( ArcSegmentState seg in vSegments ) {
				if ( seg.ContinueSelectionProgress(seg == NearestSegment) ) {
					break;
				}
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void CheckGrabGesture(IInputHand pInputHand) {
			if ( pInputHand == null ) {
				vIsGrabbing = false;
				return;
			}

			if ( vIsGrabbing && pInputHand.GrabStrength < BackReleaseThreshold ) {
				vIsGrabbing = false;
				return;
			}

			if ( !vIsGrabbing && pInputHand.GrabStrength > BackGrabThreshold ) {
				vIsGrabbing = true;
				vNavProv.Back();
				return;
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private void HandleLevelChange(int pDirection) {
			vSegments.Clear();

			NavItem[] navItems = vNavProv.GetItems();

			foreach ( NavItem navItem in navItems ) {
				var seg = new ArcSegmentState(navItem);
				vSegments.Add(seg);
			}

			OnLevelChange(pDirection);
		}

	}

}
