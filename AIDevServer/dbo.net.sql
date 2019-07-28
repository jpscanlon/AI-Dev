CREATE TABLE [dbo].[net] (
    [id]                  INT           IDENTITY (1, 1) NOT NULL,
    [name]         VARCHAR (50)  NULL,
    [activation_function] VARCHAR (50)  NOT NULL,
    [num_layers]          INT           NOT NULL,
    [training_data_path]  VARCHAR (MAX) NULL,
    [testing_data_path]   VARCHAR (MAX) NULL,
    CONSTRAINT [PK__tmp_ms_x__3213E83FEA7057E3] PRIMARY KEY CLUSTERED ([id] ASC)
);

