using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Audio;
using Assets.Scripts.BehaviourTree.Leaf.Conditions;
using Assets.Scripts.NPCs;
using Assets.Scripts.Scenario;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Assets.Scripts.Items
{

    public delegate void OnShootEvent(bool magEmpty);
    ///<inheritdoc />
    ///<summary>
    /// expansion on guninterface for player firing gun 
    ///</summary>
    public class PlayerGunInterface : GunInterface
    {
        public static List<PlayerGunInterface> AllPlayerGuns = new List<PlayerGunInterface>();

        [SerializeField] private int _maxRoundsMag = DEFAULT_MAX_ROUNDS_MAG;
        [SerializeField] private TextMeshProUGUI _lineOfFireWarning;

        private const int DEFAULT_MAX_ROUNDS_MAG = 15;

        private int _currentRoundsInMag;

        public OnShootEvent OnShoot;

        public PlayerGunInterface()
        {
            AllPlayerGuns.Add(this);
         }
                
        protected override void Start()
        {
            base.Start();
            OnShoot += OnShootEvent;
            _currentRoundsInMag = _maxRoundsMag;
        }

        private void FixedUpdate()
        {
            RaycastHit hit;
            Vector3 forward = RaycastObject.transform.TransformDirection(Vector3.up);
            if (!Physics.Raycast(RaycastObject.transform.position, forward, out hit)) return;
            //check if gun colides with npc and update statistic
            NPC target = hit.collider.transform.root.GetComponent<NPC>();
            if (target == null) return;
            if (!target.IsHostile)
                Statistics.TimeSpentAimingOnCivilians += Time.deltaTime;
            else
                Statistics.TimeSpentAimingOnHostiles += Time.deltaTime;

            if (Statistics.NpcsAimedAt != null)
            {
                if (!Statistics.NpcsAimedAt.Contains(target))
                {
                    Statistics.NpcsAimedAt.Add(target);
                }
            }
            // Time is increased by deltaTime * 2, 
            // because of the standard rate of decay (Time.deltaTime) each tick.
            target.TimeHeldAtGunpoint += Time.deltaTime * 2;
            target.NervousSource = transform.root.gameObject;
        }

       public override void Shoot()
       {
        
            if (_currentRoundsInMag > 0)
            {
                base.Shoot();
                _currentRoundsInMag--;
                if (ScenarioBase.Instance != null)
                {
                    if (ScenarioBase.Instance.Started)
                    {
                        Statistics.ShotsFired++;
                    }
                }
                AudioController.PlayAudio(gameObject, AudioCategory.GunShoot2);

            }
            else
            {
                // Gun empty
                AudioController.PlayAudio(gameObject, AudioCategory.GunTrigger);
            }
            OnShoot(_currentRoundsInMag <= 0);
        }

        public void OnShootEvent(bool empty) {
            
        }

        public void ReloadGun()
        {
            _currentRoundsInMag = _maxRoundsMag;
        }
    }
}