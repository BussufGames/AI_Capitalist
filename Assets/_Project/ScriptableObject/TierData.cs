using UnityEngine;

[CreateAssetMenu(fileName = "NewTierData", menuName = "AI Capitalist/Tier Data")]
public class TierData : ScriptableObject
{
	[Header("General Identity")]
	[SerializeField] private string _tierName;
	[SerializeField] private Sprite _tierIcon;

	[Header("Base Economy")]
	[SerializeField] private double _baseCost;
	[SerializeField] private double _baseRevenue;
	[SerializeField] private float _baseCycleTimeSeconds;
	[SerializeField] private double _costMultiplier = 1.07;

	[Header("Human Manager Config")]
	[SerializeField] private int _humanManagerShiftCycles = 5;
	[SerializeField] private int _humanManagerRestDurationInCycles = 2;
	[SerializeField] private float _humanManagerSpeedMultiplier = 1.0f;
	[SerializeField] private double _humanManagerBaseSalary;

	[Header("AI Manager Config")]
	[SerializeField] private double _aiManagerUnlockCost;
	[SerializeField] private float _aiManagerSpeedMultiplier = 0.5f;

	// Public Properties for Safe Access (Encapsulation)
	public string TierName => _tierName;
	public Sprite TierIcon => _tierIcon;
	public double BaseCost => _baseCost;
	public double BaseRevenue => _baseRevenue;
	public float BaseCycleTimeSeconds => _baseCycleTimeSeconds;
	public double CostMultiplier => _costMultiplier;

	public int HumanManagerShiftCycles => _humanManagerShiftCycles;
	public int HumanManagerRestDurationInCycles => _humanManagerRestDurationInCycles;
	public double HumanManagerBaseSalary => _humanManagerBaseSalary;
	public float HumanManagerSpeedMultiplier => _humanManagerSpeedMultiplier;

	public double AIManagerUnlockCost => _aiManagerUnlockCost;
	public float AIManagerSpeedMultiplier => _aiManagerSpeedMultiplier;
}