CREATE TABLE [dbo].[net_backup] (
    [id]                  INT          IDENTITY (1, 1) NOT NULL,
    [name]                VARCHAR (50) NOT NULL,
    [type]                VARCHAR (50) NOT NULL,
    [activation_function] VARCHAR (50) NOT NULL,
    [num_inputs]          INT          NOT NULL,
    [num_outputs]         INT          NOT NULL,
    [num_fc_layers]       INT          NOT NULL,
    [num_conv_layers]     INT          NOT NULL
);

