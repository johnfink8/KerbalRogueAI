using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MuMech;

namespace KerbalRogueAI
{
    public class TransferWindow
    {
        public double UT = 0;
        public Orbit source;
        public Orbit destination;

        AllGraphTransferCalculator worker;

        public TransferWindow(Orbit source, Orbit destination)
        {
            this.source = source;
            this.destination = destination;
            UT = Planetarium.GetUniversalTime();
            double synodic_period = source.referenceBody.orbit.SynodicPeriod(destination);
            double minDepartureTime = UT;
            double maxDepartureTime = minDepartureTime + synodic_period * 1.5;
            double minTransferTime = 3600;
            double hohmann_transfer_time = OrbitUtil.GetTransferTime(source.referenceBody.orbit, destination);
            double maxTransferTime = hohmann_transfer_time * 1.5;
            worker = new AllGraphTransferCalculator(source, destination, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, 100, 100);
        }

        public bool Check()
        {
            if (worker == null)
                return true;
            if (worker.computed == null || worker.computed.GetLength(0) < 1)
                return false;
            if (worker.Finished)
            {
                Debug.Log("worker "+ destination.ToString() + "Best date " + worker.bestDate + " best Duration " + worker.bestDuration);
                try
                {
                    UT = worker.computed[worker.bestDate, worker.bestDuration].UT;
                }
                catch (Exception)
                {
                    UT = -1;
                }
                worker.stop = true;
                worker = null;
                return true;
            }
            return false;
        }
    }
}
