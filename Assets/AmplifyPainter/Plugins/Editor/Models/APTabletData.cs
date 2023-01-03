// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;

namespace AmplifyPainter
{
	public sealed class APTabletData : ScriptableObject
	{
		[SerializeField]
		private float m_pressure;

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public float Pressure
		{
			get { return m_pressure; }
		}

		public void UpdateData()
		{
			// just update pressure for now
			if( APLibrary.GetProximity() )
				m_pressure = APLibrary.TabletGetPressure();
			else
				m_pressure = 1;
		}

		public void Hook()
		{
			APLibrary.TabletInitialize();
		}

		public void Unhook()
		{
			APLibrary.TabletFinalize();
		}

		private void OnDestroy()
		{
			Unhook();
		}
	}
}
