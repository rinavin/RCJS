using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util.fsm;

namespace com.magicsoftware.richclient.communications
{
   /// <summary>
   /// Builder class of the Client Manager's connections state machine. The purpose of this
   /// class is to consolidate all the artifacts required for building the connection state
   /// machine in one place, so that if we choose to change the building technique (such 
   /// as using XML) it will all be done here and we won't have to modify the ClientManager 
   /// class itself.
   /// </summary>
   static class ConnectionStateMachineBuilder
   {
      /// <summary>
      /// Identifiers of the connection states in the connection state machine.
      /// </summary>
      public enum ConnectionStateIdentifier
      {
         /// <summary>
         /// Denotes that no connection was attempted yet and, thus, no
         /// decision can be made regarding the connection state.
         /// </summary>
         Unknown,

         /// <summary>
         /// The client is connected to the server and is able
         /// to send requests to it.
         /// </summary>
         Connected,

         /// <summary>
         /// The client failed to access the server for any reason.
         /// </summary>
         Disconnected
      };

      static ManualTransitionTrigger connectionEstablishedTrigger;
      static ManualTransitionTrigger connectionDroppedTrigger;
  
      public static StateMachine Build()
      {
         connectionEstablishedTrigger = new ManualTransitionTrigger() { ForceImmediateTransition = true };
         connectionDroppedTrigger = new ManualTransitionTrigger() { ForceImmediateTransition = true };
         StateMachine machine = new ConnectionStateManager(ConnectionStateIdentifier.Unknown, connectionEstablishedTrigger, connectionDroppedTrigger);

         machine.AddState(CreateUnknownState());
         machine.AddState(CreateConnectedState());
         machine.AddState(CreateDisconnectedState());
         
         return machine;
      }

      private static State CreateUnknownState()
      {
         State s = new ConnectionState(ConnectionStateIdentifier.Unknown, false);
         s.AddTransition(CreateTransitionToConnected());
         s.AddTransition(CreateTransitionToDisconnected());
         return s;
      }

      private static State CreateConnectedState()
      {
         State s = new ConnectionState(ConnectionStateIdentifier.Connected, false);
         s.AddTransition(CreateTransitionToDisconnected());
         return s;
      }

      private static State CreateDisconnectedState()
      {
         State s = new ConnectionState(ConnectionStateIdentifier.Disconnected, true);
         //s.AddTransition(CreateTransitionToConnected());
         return s;
      }

      private static StateTransition CreateTransitionToDisconnected()
      {
         StateTransition st = new StateTransition(ConnectionStateIdentifier.Disconnected);
         st.AddTrigger(connectionDroppedTrigger);
         return st;
      }

      private static StateTransition CreateTransitionToConnected()
      {
         StateTransition st = new StateTransition(ConnectionStateIdentifier.Connected);
         st.AddTrigger(connectionEstablishedTrigger);
         return st;
      }
   }

   class ConnectionStateManager : StateMachine, IConnectionStateManager
   {
      public ConnectionStateManager(object startStateId, ManualTransitionTrigger connectionEstablishedTrigger, ManualTransitionTrigger connectionDroppedTrigger)
         : base(startStateId)
      {
         ConnectionDroppedTrigger = connectionDroppedTrigger;
         ConnectionEstablishedTrigger = connectionEstablishedTrigger;
      }

      ManualTransitionTrigger ConnectionEstablishedTrigger { get; set; }
      ManualTransitionTrigger ConnectionDroppedTrigger { get; set; }

      public void ConnectionDropped()
      {
         ConnectionDroppedTrigger.TriggerNow();
      }

      public void ConnectionEstablished()
      {
         ConnectionEstablishedTrigger.TriggerNow();
      }
   }

   class ConnectionState : State
   {
      bool useLocalCommandsProcessor;

      public ConnectionState(object id, bool useLocalCommandsProcessor) : base(id)
      {
         this.useLocalCommandsProcessor = useLocalCommandsProcessor;
      }

      protected override void OnEnter()
      {
         CommandsProcessorManager.SessionStatus = (useLocalCommandsProcessor
                                                      ? CommandsProcessorManager.SessionStatusEnum.Local
                                                      : CommandsProcessorManager.SessionStatusEnum.Remote);
      }

      protected override void OnLeave()
      {
         
      }
   }
}
