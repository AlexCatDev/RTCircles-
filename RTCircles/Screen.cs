namespace RTCircles
{
    public abstract class Screen : DrawableContainer
    {
        /// <summary>
        /// This gets activated when the previous screen has been unloaded and this one is getting loaded
        /// </summary>
        public virtual void OnEntering() { }
        /// <summary>
        /// This gets activated when this screen is being unloaded and a new one is being loaded
        /// </summary>
        public virtual void OnExiting() { }
        /// <summary>
        /// This gets activated when the outro transition is done playing and this screen is fully active
        /// </summary>
        public virtual void OnEnter() { }
        /// <summary>
        /// This gets activated when the intro transition is done playing and this screen is no longer active
        /// </summary>
        public virtual void OnExit() { }
    }

}
