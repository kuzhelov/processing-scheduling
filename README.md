# Actions processing scheduling
Provides utility type that aids parallel actions processing scenarios while preserving maximum allowed concurrency level. This solution aims .NET frameworks with 3.5 and 4.0 versions where asynchronous syntax constructs were not yet introduced

# Samples
Consult the Main() method of the [Program.cs](ActionsProcessingScheduling/LimitedConcurrencyActionsScheduler.cs) file

# Implementation notes
It is worth to point out that even while the 'lock' construct is kind of a subotimal one for this solution's context, this 'inefficiency' potentially could unveil itself only in situations where high contention conditions are involved - for instance, in case of tight load of quite tiny actions (where scheduling work time highly prevails on the actual processing work time). Given such kind of observation, it seems reasonable to end up with the 'lock' synchronization construct being used.
