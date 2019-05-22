UPDATE UnitPromotions SET "Level" = 1, "Column" = 0 WHERE "Column" > 0;
DELETE FROM UnitPromotionPrereqs;

INSERT INTO Modifiers(ModifierId, ModifierType) 
	SELECT 'MILITARYRESEARCH_ALL_PROMOTIONS_' || UnitType, 'MODIFIER_PLAYER_UNIT_GRANT_UNLIMITED_PROMOTION_CHOICES'
	FROM Units
	WHERE PromotionClass IS NOT NULL AND NumRandomChoices = 0;
	
INSERT INTO ModifierArguments(ModifierId, Name, "Type", "Value")
	SELECT 'MILITARYRESEARCH_ALL_PROMOTIONS_' || UnitType, 'UnitType', 'ARGTYPE_IDENTITY', UnitType
	FROM Units
	WHERE PromotionClass IS NOT NULL AND NumRandomChoices = 0;

INSERT INTO PolicyModifiers(PolicyType, ModifierId)
	SELECT 'POLICY_MILITARY_RESEARCH', 'MILITARYRESEARCH_ALL_PROMOTIONS_' || UnitType
	FROM Units
	WHERE PromotionClass IS NOT NULL AND NumRandomChoices = 0;;

UPDATE Units SET NumRandomChoices = 3 WHERE PromotionClass IS NOT NULL AND NumRandomChoices = 0;