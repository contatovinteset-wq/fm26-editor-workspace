using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public interface IExportHandler
    {
        /// <summary>
        /// Checks if this handler can run on the provided root and returns whether it can.
        /// If it can, it prepares its internal state (like finding the tables, etc).
        /// </summary>
        bool TryStartCapture(VisualElement root, out string errorMessage);
        
        /// <summary>
        /// Performs one capture step (e.g. read current visible rows, scroll down).
        /// Returns true if the capture is complete, false if more steps are needed.
        /// </summary>
        bool CaptureStep();
        
        /// <summary>
        /// Finalizes the capture, writes files, etc.
        /// </summary>
        void FinishCapture();

        /// <summary>
        /// Deep cleanup of UI references to avoid crashes on application quit.
        /// </summary>
        void Cleanup();
    }
}
