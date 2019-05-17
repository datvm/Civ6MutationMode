DROP TABLE IF EXISTS OriginalLeaderTraits;
DROP TABLE IF EXISTS OriginalCivilizationTraits;
CREATE TABLE OriginalLeaderTraits(LeaderType TEXT NOT NULL, TraitType TEXT NOT NULL, PRIMARY KEY(LeaderType, TraitType));
CREATE TABLE OriginalCivilizationTraits(CivilizationType TEXT NOT NULL, TraitType TEXT NOT NULL, PRIMARY KEY(CivilizationType, TraitType));
INSERT INTO OriginalLeaderTraits SELECT LeaderType, TraitType FROM LeaderTraits;
INSERT INTO OriginalCivilizationTraits SELECT CivilizationType, TraitType FROM CivilizationTraits;

INSERT OR IGNORE INTO GlobalParameters (Name, "Value") VALUES ('MUTATION_RANDOM_SEED', STRFTIME("%d%H%M%S"));

DROP TABLE IF EXISTS OriginalValidLeaders;
DROP TABLE IF EXISTS OriginalLeaderTraits;
DROP TABLE IF EXISTS TraitsSwap;

CREATE TABLE IF NOT EXISTS OriginalValidLeaders("LeaderType" TEXT NOT NULL, "Seed" INTEGER NOT NULL DEFAULT 0, "Max" INTEGER NOT NULL DEFAULT 0, "SwapWith" INTEGER NOT NULL DEFAULT 1, PRIMARY KEY(LeaderType));
CREATE TABLE IF NOT EXISTS OriginalLeaderTraits("LeaderType" TEXT NOT NULL, "TraitType" TEXT NOT NULL, PRIMARY KEY(LeaderType, TraitType));
CREATE TABLE IF NOT EXISTS TraitsSwap("OriginalLeaderType" TEXT NOT NULL, "TargetLeaderType" TEXT NOT NULL, PRIMARY KEY(OriginalLeaderType));

INSERT INTO OriginalValidLeaders("LeaderType")
	SELECT L.LeaderType
	FROM Leaders L
	WHERE L.InheritFrom = 'LEADER_DEFAULT';

UPDATE OriginalValidLeaders SET
	Seed = (SELECT "Value" FROM GlobalParameters WHERE Name = 'MUTATION_RANDOM_SEED'),
	"Max" = (SELECT COUNT(*) FROM OriginalValidLeaders);

UPDATE OriginalValidLeaders SET
	SwapWith = ((((Seed * ROWID) % 79) * 53) % "Max") + 1;

INSERT INTO OriginalLeaderTraits("LeaderType", "TraitType")
	SELECT LT."LeaderType", LT."TraitType"
	FROM LeaderTraits LT
	WHERE LT.LeaderType IN
	(
		SELECT LeaderType
		FROM OriginalValidLeaders L
	);
	
DELETE FROM LeaderTraits
WHERE LeaderType IN
(
	SELECT LeaderType
	FROM OriginalValidLeaders
);

INSERT INTO TraitsSwap("OriginalLeaderType", "TargetLeaderType")
	SELECT L.LeaderType, R.LeaderType
	FROM OriginalValidLeaders L
	LEFT JOIN
		(
			SELECT LeaderType, SwapWith, rowid
			FROM OriginalValidLeaders
		) R
	ON L.SwapWith = R.rowid;

INSERT INTO LeaderTraits("LeaderType", "TraitType")
	SELECT OriginalLeaderType, TraitType
	FROM TraitsSwap TS
	LEFT JOIN OriginalLeaderTraits LT
	ON TS.TargetLeaderType = LT.LeaderType;