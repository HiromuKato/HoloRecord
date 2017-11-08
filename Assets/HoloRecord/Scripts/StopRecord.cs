using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

namespace HoloRecord
{
    public class StopRecord : MonoBehaviour, IInputClickHandler
    {
        public RecordController RecordController;

        public void OnInputClicked(InputClickedEventData eventData)
        {
            RecordController.StopRecord();
        }

    } // class
} // namespace
