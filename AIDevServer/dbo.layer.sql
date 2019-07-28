CREATE TABLE [dbo].[layer] (
    [net_id]      INT NOT NULL,
    [layer_num]   INT NOT NULL,
    [num_outputs] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([net_id] ASC, [layer_num] ASC)
);

