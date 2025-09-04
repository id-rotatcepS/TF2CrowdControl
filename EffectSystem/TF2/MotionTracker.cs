using System.Numerics;
using System.Text.RegularExpressions;


namespace EffectSystem.TF2
{
    /// <summary>
    /// Keep track of user speed, etc. based on "getpos" output.
    /// </summary>
    public class MotionTracker
    {
        private TF2Proxy tf2;

        public MotionTracker(TF2Proxy tf2Proxy)
        {
            this.tf2 = tf2Proxy;
        }

        /// <summary>
        /// setpos x, y, and z
        /// </summary>
        private Regex setposxyz = new Regex(@".*setpos\s+(?<spx>\-?\d+(\.\d+)?)\s+(?<spy>\-?\d+(\.\d+)?)\s+(?<spz>\-?\d+(\.\d+)?).*", RegexOptions.Singleline);
        /// <summary>
        /// setang pitch, yaw, and roll
        /// last value is 0.00000... so that's roll.
        /// </summary>
        private Regex setangpyr = new Regex(@".*setang\s+(?<sap>\-?\d+(\.\d+)?)\s+(?<say>\-?\d+(\.\d+)?)\s+(?<sar>\-?\d+(\.\d+)?).*", RegexOptions.Singleline);

        //https://developer.valvesoftware.com/wiki/Getpos
        // The output format is "setpos <x> <y> <z>;setang <pitch> <yaw> <roll>".
        //Important:
        //The difference between getpos and getpos_exact (= getpos 2) is the z-height:
        // The former returns the local player's eye position and the latter their origin.
        // These locations are identical for spectators but not for regular players.
        // If you are not spectating and you want to reproduce your current location,
        // you have to use getpos_exact and not getpos. For spectators, both commands return the same result.
        //The difference between setpos and setpos_exact is just whether or not
        // entities will react to the player's new position. Both commands always
        // set the player's origin, so if you are not spectating, your eye position
        // will have a higher z-height than the z-coordinate that you have entered.
        //] getpos
        //setpos 8415.820313 -5248.870117 320.000000;setang -70.642090 71.564941 0.000000
        //If you don't want the setpos and setang in the output, use spec_pos.
        //] spec_pos
        //spec_goto 8415.8 -5248.9 320.0 -70.6 71.6

        //getpos 	Prints the local player's eye position and angles. Invoking the output will put a spectator into the same position.
        //getpos 2 	Prints the local player's origin and angles. Invoking the output will put any player into the same position.
        //Bug:
        //The returned pitch is always 0. Use getpos to actually obtain that value.  [todo tested in ?]
        //getpos_exact (in all games since
        //) 	Equivalent to getpos 2.
        //spec_pos 	Equivalent to getpos and getpos 2 respectively but prints the values in the format "spec_goto <x> <y> <z> <pitch> <yaw>" and with just one decimal place for each of the five numbers. The roll value is omitted.
        //Does not print the prefix "spec_goto".
        //spec_pos 2


        private DateTime lastMotionTime = DateTime.MinValue;
        private TimeSpan lastTimePeriod = TimeSpan.Zero;
        private static readonly Vector3 zzz = new Vector3();
        private static readonly Vector3 stopped = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 lastPosition = zzz;
        private Vector3 lastDistance = stopped;

        public void RecordUserMotion()
        {
            string? getpos;
            lock (this)
            {
                getpos = tf2.GetValue("getpos");
            }

            if (string.IsNullOrWhiteSpace(getpos))
                return;

            DateTime updateTime = DateTime.Now;

            Match pos = setposxyz.Match(getpos);
            if (pos.Success)
            {
                string spx = pos.Groups["spx"].Value;
                string spy = pos.Groups["spy"].Value;
                string spz = pos.Groups["spz"].Value;

                UpdateDistanceAndPosition(spx, spy, spz);

                #region angle
                // Future...not sure this is right, and don't need it right now.
                //Match ang = setangpyr.Match(getpos);
                //if (ang.Success)
                //{
                //    string sap = ang.Groups["sap"].Value;
                //    string say = ang.Groups["say"].Value;
                //    string sar = ang.Groups["sar"].Value;
                //    //misuse of vector? as long as we use it consistently I guess we're fine.
                //    System.Windows.Media.Media3D.Vector3D currentangle = new System.Windows.Media.Media3D
                //        .Vector3D(double.Parse(sap), double.Parse(say), double.Parse(sar));

                //    if (!lastangle.Equals(straight))
                //    {
                //        System.Windows.Media.Media3D.Vector3D rotation = System.Windows.Media.Media3D.Point3D
                //            .Subtract(currentangle, lastangle);
                //        lastrotation = rotation;
                //    }
                //    lastangle = currentangle;
                //}
                #endregion angle

                lastTimePeriod = updateTime.Subtract(lastMotionTime);
                lastMotionTime = updateTime;
            }
        }

        private void UpdateDistanceAndPosition(string spx, string spy, string spz)
        {
            Vector3 currentposition = new Vector3(float.Parse(spx), float.Parse(spy), float.Parse(spz));

            lastDistance = GetLastDistance(currentposition);

            lastPosition = currentposition;
        }

        private Vector3 GetLastDistance(Vector3 currentposition)
        {
            if (lastMotionTime == DateTime.MinValue
                || lastPosition.Equals(zzz)
                || currentposition.Equals(zzz))
                return stopped;

            // start + distance = end  therefore  distance = end - start
            Vector3 distance = Vector3.Subtract(currentposition, lastPosition);

            return distance;
        }

        public double GetVerticalSpeed()
        {
            if (lastTimePeriod.TotalSeconds == 0)
                return 0;

            double speed = lastDistance.Z / lastTimePeriod.TotalSeconds;
            return speed;
        }

        public double GetHorizontalSpeed()
        {
            if (lastTimePeriod.TotalSeconds == 0)
                return 0;

            double distance = Math.Sqrt(lastDistance.X * lastDistance.X + lastDistance.Y * lastDistance.Y);
            double speed = distance / lastTimePeriod.TotalSeconds;
            return speed;
        }
    }
}