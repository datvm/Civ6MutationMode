DROP TABLE IF EXISTS OriginalPolicyPrereqCivic;
DROP TABLE IF EXISTS MutationSwappedPolicyPrereqCivic;

CREATE TABLE IF NOT EXISTS OriginalValidPolicyPrereqCivic(PolicyType TEXT NOT NULL, PrereqCivic TEXT NOT NULL, "Seed" INTEGER NOT NULL DEFAULT 0, "Max" INTEGER NOT NULL DEFAULT 0, "SwapWith" INTEGER NOT NULL DEFAULT 1, SwappedPrereqCivic TEXT, PRIMARY KEY(PolicyType));
CREATE TABLE IF NOT EXISTS MutationSwappedPolicyPrereqCivic(PolicyType TEXT NOT NULL, SwappedPrereqCivic TEXT NOT NULL, PRIMARY KEY(PolicyType));

INSERT INTO OriginalValidPolicyPrereqCivic(PolicyType, PrereqCivic)
	SELECT PolicyType, PrereqCivic
	FROM Policies
	WHERE PrereqCivic IS NOT NULL
	ORDER BY PolicyType;

UPDATE OriginalValidPolicyPrereqCivic SET
	Seed = (SELECT "Value" FROM GlobalParameters WHERE Name = 'MUTATION_RANDOM_SEED'),
	"Max" = (SELECT COUNT(*) FROM OriginalValidPolicyPrereqCivic);

UPDATE OriginalValidPolicyPrereqCivic SET
	SwapWith = ((((Seed * ROWID) % 79) * 53) % "Max") + 1;

INSERT INTO MutationSwappedPolicyPrereqCivic(PolicyType, SwappedPrereqCivic)
	SELECT L.PolicyType, R.PrereqCivic
	FROM OriginalValidPolicyPrereqCivic L
	LEFT JOIN
		(
			SELECT PrereqCivic, rowid
			FROM OriginalValidPolicyPrereqCivic
		) R
	ON L.SwapWith = R.rowid;
	
UPDATE Policies SET PrereqCivic = 
	(SELECT SwappedPrereqCivic FROM MutationSwappedPolicyPrereqCivic S WHERE S.PolicyType = Policies.PolicyType)
WHERE PrereqCivic IS NOT NULL;