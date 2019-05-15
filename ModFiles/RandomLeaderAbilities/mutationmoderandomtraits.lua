local currentSeed = GameConfiguration.GetValue("TraitsSwapSeed");

if (not currentSeed) then
	currentSeed = os.time();
	GameConfiguration.SetValue("TraitsSwapSeed", currentSeed);
end

math.randomseed(currentSeed);

local leaders = {};
for leader in GameInfo.Leaders() do
	if (leader.InheritFrom == 'LEADER_DEFAULT') then
		local leaderInfo = 
		{
			LeaderType = leader.LeaderType,
			Traits = {},
		};
		
		leaders[leader.LeaderType] = leaderInfo;
	end
end

for trait in GameInfo.LeaderTraits() do
	local leader = leaders[trait.LeaderType];
	
	if (leader) then
		table.insert(leader.Traits, trait);
	end
end