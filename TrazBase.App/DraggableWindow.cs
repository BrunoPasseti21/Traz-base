namespace TrazBase.App;

internal static class DraggableWindow
{
    public static void Wire(Control control, Form form)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(form);

        var state = new DragState();

        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || control.IsDisposed || !control.IsHandleCreated || form.IsDisposed || !form.IsHandleCreated) return;
            state.Dragging = true;
            state.Offset = new Point(Cursor.Position.X - form.Left, Cursor.Position.Y - form.Top);
            control.Capture = true;
        };

        control.MouseMove += (_, e) =>
        {
            if (!state.Dragging || control.IsDisposed || !control.IsHandleCreated || form.IsDisposed || !form.IsHandleCreated) return;
            form.Location = new Point(Cursor.Position.X - state.Offset.X, Cursor.Position.Y - state.Offset.Y);
        };

        control.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || control.IsDisposed || !control.IsHandleCreated || form.IsDisposed || !form.IsHandleCreated) return;
            state.Dragging = false;
            control.Capture = false;
        };

        control.MouseLeave += (_, _) =>
        {
            if (control.IsDisposed || !control.IsHandleCreated || form.IsDisposed || !form.IsHandleCreated)
            {
                state.Dragging = false;
                return;
            }
            state.Dragging = false;
            control.Capture = false;
        };
    }

    private sealed class DragState
    {
        public bool Dragging;
        public Point Offset;
    }
}
