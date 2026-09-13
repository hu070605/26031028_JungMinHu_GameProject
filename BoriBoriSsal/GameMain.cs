using System.Windows.Forms;
using Vortice.DirectWrite;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
	private enum GameState { Title, FirstBori, BoriPause, SecondBori, Timing, Success, GameOver }

	private const float BarLeft = 91.0f;
	private const float BarWidth = 778.0f;
	private const float ClearStart = 500.0f;
	private const float ClearWidth = 156.0f;

	private GameState _state = GameState.Title;
	private int _stage = 1;
	private int _bestStage;
	private double _stateTime;
	private float _markerPosition;
	private float _markerDirection = 1.0f;

	private G2Texture? _titleBackground;
	private G2Texture? _titleLogo;
	private G2Texture? _titleStartButton;
	private G2Texture? _titleExitButton;
	private G2Texture? _titleBestScore;
	private G2Texture? _gameplayScene;
	private G2Texture? _gameplayChant;
	private G2Texture? _stageBoard;
	private G2Texture? _timingMarker;
	private G2Texture? _gameOverScene;
	private G2Font? _numberFont;
	private G2Font? _smallNumberFont;

	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	protected override void Initialize()
	{
		ClearColor = new Color4(0.07f, 0.04f, 0.02f, 1.0f);
		_bestStage = LoadBestStage();

		_titleBackground = new G2Texture("resource/gametitle/title_bg.png");
		_titleLogo = new G2Texture("resource/gametitle/title_boribori.png");
		_titleStartButton = new G2Texture("resource/gametitle/title_game_start.png");
		_titleExitButton = new G2Texture("resource/gametitle/title_game_exit.png");
		_titleBestScore = new G2Texture("resource/gametitle/title_best_score_blank.png");

		_gameplayScene = new G2Texture("resource/gameplay/gameplay_scene.png");
		_gameplayChant = new G2Texture("resource/gameplay/gameplay_boribori.png");
		_stageBoard = new G2Texture("resource/gameplay/gameplay_stage.png");
		_timingMarker = new G2Texture("resource/gameplay/gameplay_timing-_marker.png");

		_gameOverScene = new G2Texture("resource/gameover/gameover_scene.png");
		_numberFont = CreateFont(30);
		_smallNumberFont = CreateFont(22);
	}

	protected override void Update()
	{
		_stateTime += DeltaTime;
		if (Input.IsKeyDown(Keys.Escape)) { Close(); return; }

		switch (_state)
		{
			case GameState.Title:
				if (Input.IsKeyDown(Keys.Space)) StartGame();
				break;
			case GameState.FirstBori:
				if (_stateTime >= 0.58) ChangeState(GameState.BoriPause);
				break;
			case GameState.BoriPause:
				if (_stateTime >= 0.14) ChangeState(GameState.SecondBori);
				break;
			case GameState.SecondBori:
				if (_stateTime >= 0.58) BeginTiming();
				break;
			case GameState.Timing:
				UpdateTiming();
				break;
			case GameState.Success:
				if (_stateTime >= 0.65) { _stage++; ChangeState(GameState.FirstBori); }
				break;
			case GameState.GameOver:
				if (Input.IsKeyDown(Keys.Space) || Input.IsKeyDown(Keys.Enter)) StartGame();
				break;
		}
	}

	private void StartGame()
	{
		_stage = 1;
		ChangeState(GameState.FirstBori);
	}

	private void BeginTiming()
	{
		_markerPosition = BarLeft;
		_markerDirection = 1.0f;
		ChangeState(GameState.Timing);
	}

	private void UpdateTiming()
	{
		float speed = Math.Min(760.0f, 250.0f + (_stage - 1) * 28.0f);
		_markerPosition += _markerDirection * speed * (float)DeltaTime;
		if (_markerPosition >= BarLeft + BarWidth)
		{
			_markerPosition = BarLeft + BarWidth;
			_markerDirection = -1.0f;
		}
		else if (_markerPosition <= BarLeft)
		{
			_markerPosition = BarLeft;
			_markerDirection = 1.0f;
		}

		if (!Input.IsKeyDown(Keys.Space)) return;

		bool cleared = _markerPosition >= ClearStart && _markerPosition <= ClearStart + ClearWidth;
		if (!cleared)
		{
			ChangeState(GameState.GameOver);
			return;
		}

		if (_stage > _bestStage)
		{
			_bestStage = _stage;
			SaveBestStage();
		}
		ChangeState(GameState.Success);
	}

	private void ChangeState(GameState state)
	{
		_state = state;
		_stateTime = 0.0;
	}

	protected override void Render()
	{
		if (_state == GameState.Title)
		{
			_titleBackground?.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));
			RenderTitle();
			return;
		}

		if (_state == GameState.GameOver)
		{
			_gameOverScene?.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));
			RenderGameOverScores();
			return;
		}

		_gameplayScene?.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));
		RenderStage();
		RenderChant();
		if (_state is GameState.Timing or GameState.Success) RenderTimingMarker();
	}

	private void RenderTitle()
	{
		_titleLogo?.Draw(new Rect(165, 75, 630, 217), new Rect(0, 0, 2137, 736));
		_titleStartButton?.Draw(new Rect(330, 340, 300, 104), new Rect(0, 0, 2126, 740));
		_titleExitButton?.Draw(new Rect(370, 455, 220, 58), new Rect(0, 0, 1845, 490));
		_titleBestScore?.Draw(new Rect(350, 540, 260, 90), new Rect(0, 0, 2129, 739));
		_smallNumberFont?.DrawText(_bestStage.ToString(), new Rect(474, 560, 36, 40), new Color4(1.0f, 0.72f, 0.06f, 1.0f));
	}

	private void RenderStage()
	{
		_stageBoard?.Draw(new Rect(350, 16, 260, 59), new Rect(0, 0, 1811, 409));
		_numberFont?.DrawText(_stage.ToString(), new Rect(511, 25, 48, 38), new Color4(1.0f, 0.93f, 0.72f, 1.0f));
	}

	private void RenderChant()
	{
		if (_state is not (GameState.FirstBori or GameState.BoriPause or GameState.SecondBori or GameState.Timing)) return;

		float pulse = 1.0f + 0.025f * MathF.Sin((float)_stateTime * 9.0f);
		float width = 324.0f * pulse;
		float height = 127.0f * pulse;
		_gameplayChant?.Draw(
			new Rect(480.0f - width * 0.5f, 164.0f - height * 0.5f, width, height),
			new Rect(0, 0, 1673, 654));
	}

	private void RenderTimingMarker()
	{
		_timingMarker?.Draw(new Rect(_markerPosition - 9, 506, 18, 84), new Rect(0, 0, 276, 1286));
	}

	private void RenderGameOverScores()
	{
		_numberFont?.DrawText(_stage.ToString(), new Rect(548, 170, 80, 45), new Color4(0.25f, 0.11f, 0.02f, 1.0f));
		_numberFont?.DrawText(_bestStage.ToString(), new Rect(548, 229, 80, 45), new Color4(0.25f, 0.11f, 0.02f, 1.0f));
	}

	private static string BestStagePath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"BoriBoriSsal",
		"best-stage.txt");

	private static int LoadBestStage()
	{
		try { return File.Exists(BestStagePath) && int.TryParse(File.ReadAllText(BestStagePath), out int value) ? value : 0; }
		catch { return 0; }
	}

	private void SaveBestStage()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(BestStagePath)!);
			File.WriteAllText(BestStagePath, _bestStage.ToString());
		}
		catch { /* 기록 저장 실패는 플레이를 중단시키지 않는다. */ }
	}

	private static G2Font CreateFont(float size) => new(
		"Malgun Gothic", size, FontWeight.Bold, Vortice.DirectWrite.FontStyle.Normal,
		TextAlignment.Center, ParagraphAlignment.Center);

	public override void Dispose()
	{
		_smallNumberFont?.Dispose();
		_numberFont?.Dispose();
		_gameOverScene?.Dispose();
		_timingMarker?.Dispose();
		_stageBoard?.Dispose();
		_gameplayChant?.Dispose();
		_gameplayScene?.Dispose();
		_titleBestScore?.Dispose();
		_titleExitButton?.Dispose();
		_titleStartButton?.Dispose();
		_titleLogo?.Dispose();
		_titleBackground?.Dispose();
		base.Dispose();
	}
}
