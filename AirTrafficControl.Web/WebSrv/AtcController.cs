﻿using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AtcController
    {
        public async Task<IEnumerable<AirplaneStateModel>> GetFlyingAirplaneStates()
        {
            try
            {
                var retval = new List<AirplaneStateModel>();
                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                var flyingAirplaneIDs = await atc.GetFlyingAirplaneIDs();

                if (flyingAirplaneIDs != null)
                {
                    foreach(string airplaneID in flyingAirplaneIDs)
                    {
                        var airplane = ActorProxy.Create<IAirplane>(new ActorId(airplaneID));
                        var airplaneActorState = await airplane.GetState();
                        var airplaneState = airplaneActorState.AirplaneState;

                        var stateModel = new AirplaneStateModel(airplaneID, airplaneState.ToString(), airplaneState.Location, airplaneState.GetHeading(airplaneActorState.FlightPlan));
                        retval.Add(stateModel);
                    }
                }

                return retval;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("GetFlyingAirplaneIDs", e.ToString());
                throw;
            }
        }

        public async Task<AirplaneActorState> GetAirplaneState(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Airplane ID should not be empty", "id");
                }

                var airplane = ActorProxy.Create<IAirplane>(new ActorId(id));
                return await airplane.GetState();
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("GetAirplaneState", e.ToString());
                throw;
            }
        }

        public async Task StartNewFlight(string airplaneID, string departurePoint, string destination)
        {
            try
            {
                FlightPlan flightPlan = new FlightPlan();
                flightPlan.AirplaneID = airplaneID;
                flightPlan.DeparturePoint = Universe.Current.Airports.Where(a => a.Name == "KSEA").First();
                flightPlan.Destination = Universe.Current.Airports.Where(a => a.Name == "KPDX").First();
                flightPlan.Validate();

                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                await atc.StartNewFlight(flightPlan);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("StartNewFlight", e.ToString());
                throw;
            }
        }

        public Task<IEnumerable<Airport>> GetAirports()
        {
            return Task.FromResult(Universe.Current.Airports.AsEnumerable());
        }
    }
}
