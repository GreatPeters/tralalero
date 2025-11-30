using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{

    [CreateAssetMenu(fileName = "Enemy", menuName = "Enemy/Create New Enemy Type")]
    public class EnemySO : ScriptableObject
    {
        [Header("Enemy Stats")]

        [Tooltip("The maximum health of the enemy.")]
        public float enemyHealth;

        [Tooltip("The amount of damage the enemy deals to the player or other objects.")]
        public float enemyDamage;

        [Tooltip("The movement speed of the enemy.")]
        public float enemySpeed;

        [Tooltip("Score awarded to the player when this enemy is killed.")]
        public int scoreUponDeath;

        [Tooltip("Speed at which to rotate toward player")]
        public float turnSpeed;

        [Header("Pool Options")]

        [Tooltip("The type of this enemy (used by pooling system).")]
        public EnemyType enemyType;

        [Tooltip("Prefab representing the enemy GameObject.")]
        public GameObject enemyPrefab;

        [Tooltip("Number of instances of this enemy to create in the object pool.")]
        public int poolSize;

        [Header("Dependencies")]
        [Tooltip("Visual shown when this enemy is hit.")]
        public GameObject enemyHitVFX;
        public GameObject enemyDeathVFX;

        [Tooltip("Sound played when this enemy is hit.")]
        public AudioClip enemyHitSound;
        public AudioClip enemyDeathSound;

    }
}