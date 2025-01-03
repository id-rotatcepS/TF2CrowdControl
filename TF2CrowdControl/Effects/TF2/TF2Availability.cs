﻿namespace Effects.TF2
{
    public interface TF2Availability
    {
        /// <summary>
        /// Checks whether a tested availability passes with the current tf2 proxy.
        /// Defaults to false if there is any error or exception in the test.
        /// </summary>
        /// <param name="tF2Instance"></param>
        /// <returns></returns>
        bool IsAvailable(TF2Proxy tF2Instance);
    }

    internal class AliveClass : AliveInMap
    {
        private string userClass;

        public AliveClass(string userClass)
        {
            this.userClass = userClass;
        }

        override public bool IsAvailable(TF2Proxy tF2Instance)
        {
            try
            {
                return tF2Instance?.ClassSelection == userClass
                    && base.IsAvailable(tF2Instance);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal class AliveClassForMinimumTime : AliveClass
    {
        public AliveClassForMinimumTime(string userClass, double seconds)
            : base(userClass)
        {
            Seconds = seconds;
        }

        public double Seconds { get; }

        override public bool IsAvailable(TF2Proxy tF2Instance)
        {
            try
            {
                return DateTime.Now.Subtract(tF2Instance?.UserSpawnTime ?? DateTime.MinValue) > TimeSpan.FromSeconds(Seconds)
                    && base.IsAvailable(tF2Instance);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal class AliveInMap : InMap
    {
        override public bool IsAvailable(TF2Proxy tF2Instance)
        {
            try
            {
                return tF2Instance?.IsUserAlive ?? false
                    && base.IsAvailable(tF2Instance);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal class InMap : InApplication
    {
        override public bool IsAvailable(TF2Proxy tf2StatusProxy)
        {
            // status system that queries this stuff regularly and this class just asks for latest answer.
            if (tf2StatusProxy == null)
                return false;

            return tf2StatusProxy.IsMapLoaded;
        }
    }

    internal class InApplication : TF2Availability
    {
        virtual public bool IsAvailable(TF2Proxy tf2StatusProxy)
        {
            // status system that queries this stuff regularly and this class just asks for latest answer.
            if (tf2StatusProxy == null)
                return false;

            return tf2StatusProxy.IsOpen;
        }
    }

}