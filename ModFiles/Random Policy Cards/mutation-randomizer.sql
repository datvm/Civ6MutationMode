-- You can use one of the following 3 options for randomness. Please use only one of these.
-- Which ever you want to use, remove the -- before INSERT word and put -- before the others.

-- Use this one if you want consistent seed (meaning the swap will always be the same for the same seed
-- Change the second value to any random seed number you want
-- INSERT OR IGNORE INTO GlobalParameters (Name, "Value") VALUES ('MUTATION_RANDOM_SEED', "123456789");

-- Use this one if you want the seed to change every time the game save/load
-- INSERT OR IGNORE INTO GlobalParameters (Name, "Value") VALUES ('MUTATION_RANDOM_SEED', STRFTIME("%d%H%M%S"));

-- Use this one if you want the seed to change every day 
INSERT OR IGNORE INTO GlobalParameters (Name, "Value") VALUES ('MUTATION_RANDOM_SEED', STRFTIME("%Y%m%d"));