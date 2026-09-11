using System.Numerics;
using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
	private enum GameState { Title, FirstBori, BoriPause, SecondBori, Timing, Success, Caught, GameOver }

	private const float BarLeft = 150.0f;
	private const float BarTop = 540.0f;
	private const float BarWidth = 660.0f;
	private const float BarHeight = 34.0f;
	private const float ClearWidth = 120.0f;

	private GameState _state = GameState.Title;
	private int _stage = 1;
	private int _bestStage;
	private double _stateTime;
	private float _markerPosition;
	private float _markerDirection = 1.0f;
	private float _clearStart;
	private readonly Random _random = new();

	private G2Texture? _background;
	private G2Font? _titleFont;
	private G2Font? _chantFont;
	private G2Font? _headingFont;
	private G2Font? _bodyFont;
	private ID2D1SolidColorBrush? _darkBrush;
	private ID2D1SolidColorBrush? _panelBrush;
	private ID2D1SolidColorBrush? _creamBrush;
	private ID2D1SolidColorBrush? _goldBrush;
	private ID2D1SolidColorBrush? _greenBrush;
	private ID2D1SolidColorBrush? _redBrush;
	private ID2D1SolidColorBrush? _skinBrush;
	private ID2D1SolidColorBrush? _skinShadeBrush;

	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	protected override void Initialize()
	{
		ClearColor = new Color4(0.85f, 0.94f, 1.0f, 1.0f);
		_bestStage = LoadBestStage();
		_background = new G2Texture("resource/boribori-background.png");
		_titleFont = CreateFont(74);
		_chantFont = CreateFont(66);
		_headingFont = CreateFont(30);
		_bodyFont = CreateFont(22);

		_darkBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.15f, 0.13f, 0.10f, 1.0f));
		_panelBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.08f, 0.08f, 0.06f, 0.78f));
		_creamBrush = RenderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.96f, 0.79f, 1.0f));
		_goldBrush = RenderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.70f, 0.12f, 1.0f));
		_greenBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.18f, 0.76f, 0.35f, 1.0f));
		_redBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.93f, 0.24f, 0.20f, 1.0f));
		_skinBrush = RenderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.72f, 0.50f, 1.0f));
		_skinShadeBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.72f, 0.38f, 0.23f, 1.0f));
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
				if (_stateTime >= 1.55) { _stage++; ChangeState(GameState.FirstBori); }
				break;
			case GameState.Caught:
				if (_stateTime >= 1.35) ChangeState(GameState.GameOver);
				break;
			case GameState.GameOver:
				if (Input.IsKeyDown(Keys.Space)) StartGame();
				break;
		}
	}

	private void StartGame() { _stage = 1; ChangeState(GameState.FirstBori); }

	private void BeginTiming()
	{
		_clearStart = BarLeft + 45.0f + (float)_random.NextDouble() * (BarWidth - ClearWidth - 90.0f);
		_markerPosition = BarLeft;
		_markerDirection = 1.0f;
		ChangeState(GameState.Timing);
	}

	private void UpdateTiming()
	{
		float speed = Math.Min(760.0f, 250.0f + (_stage - 1) * 28.0f);
		_markerPosition += _markerDirection * speed * (float)DeltaTime;
		if (_markerPosition >= BarLeft + BarWidth) { _markerPosition = BarLeft + BarWidth; _markerDirection = -1.0f; }
		else if (_markerPosition <= BarLeft) { _markerPosition = BarLeft; _markerDirection = 1.0f; }

		if (Input.IsKeyDown(Keys.Space))
		{
			bool cleared = _markerPosition >= _clearStart && _markerPosition <= _clearStart + ClearWidth;
			if (cleared)
			{
				if (_stage > _bestStage) { _bestStage = _stage; SaveBestStage(); }
				ChangeState(GameState.Success);
			}
			else ChangeState(GameState.Caught);
		}
	}

	private void ChangeState(GameState state) { _state = state; _stateTime = 0.0; }

	protected override void Render()
	{
		_background?.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));
		RenderTopBar();
		if (_state == GameState.Title) { RenderTitle(); return; }
		if (_state == GameState.GameOver) { RenderHands(1.0f, 1.0f); RenderGameOver(); return; }

		float fistProgress = _state == GameState.Success ? GetPunchProgress() : 0.0f;
		float caughtAmount = 0.0f;
		if (_state == GameState.Caught)
		{
			fistProgress = Math.Min(1.0f, (float)_stateTime * 2.8f);
			caughtAmount = Math.Min(1.0f, (float)_stateTime * 3.5f);
		}
		RenderHands(caughtAmount, fistProgress);
		RenderChant();
		if (_state is GameState.Timing or GameState.Success or GameState.Caught) RenderTimingBar();
	}

	private void RenderTopBar()
	{
		RenderTarget.FillRectangle(new Rect(0, 0, 960, 76), _panelBrush!);
		_headingFont?.DrawText($"STAGE {_stage}", new Rect(28, 17, 250, 45), new Color4(1.0f, 0.94f, 0.66f, 1.0f));
		_headingFont?.DrawText($"BEST {_bestStage}", new Rect(680, 17, 250, 45), new Color4(1.0f, 0.94f, 0.66f, 1.0f));
	}

	private void RenderTitle()
	{
		RenderTarget.FillRectangle(new Rect(110, 120, 740, 405), _panelBrush!);
		_titleFont?.DrawText("보리보리쌀", new Rect(130, 150, 700, 100), new Color4(1.0f, 0.76f, 0.18f, 1.0f));
		_headingFont?.DrawText("타이밍을 맞춰 주먹을 재빨리 빼세요!", new Rect(155, 278, 650, 55), new Color4(1, 1, 1, 1));
		RenderTarget.FillRectangle(new Rect(275, 370, 410, 78), _goldBrush!);
		_headingFont?.DrawText("SPACE  게임 시작", new Rect(295, 385, 370, 50), new Color4(0.15f, 0.12f, 0.06f, 1.0f));
		_bodyFont?.DrawText("ESC 종료  ·  ALT+ENTER 전체화면", new Rect(250, 475, 460, 38), new Color4(0.92f, 0.92f, 0.92f, 1.0f));
	}

	private void RenderChant()
	{
		string text = _state switch
		{
			GameState.FirstBori or GameState.SecondBori => "보리!",
			GameState.Timing => "지금이다!",
			GameState.Success => "쌀!",
			GameState.Caught => "쌀..!",
			_ => string.Empty
		};
		Color4 color = _state == GameState.Caught ? new Color4(0.95f, 0.25f, 0.20f, 1.0f) : new Color4(1.0f, 0.67f, 0.08f, 1.0f);
		_chantFont?.DrawText(text, new Rect(250, 90, 460, 90), color);
		if (_state == GameState.Success) _headingFont?.DrawText("CLEAR!", new Rect(330, 450, 300, 48), new Color4(0.12f, 0.72f, 0.28f, 1.0f));
		else if (_state == GameState.Caught) _headingFont?.DrawText("잡혔다!", new Rect(330, 450, 300, 48), new Color4(0.92f, 0.18f, 0.15f, 1.0f));
	}

	private void RenderHands(float caughtAmount, float fistProgress)
	{
		float leftX = 182.0f + caughtAmount * 80.0f;
		float rightX = 658.0f - caughtAmount * 80.0f;
		DrawAiHand(leftX, 230, false);
		DrawAiHand(rightX, 230, true);
		DrawPlayerFist(480, 415.0f - fistProgress * 145.0f, _state == GameState.Caught);
	}

	private void DrawAiHand(float x, float y, bool mirrored)
	{
		float palmX = mirrored ? x : x + 45;
		RenderTarget.FillRectangle(new Rect(palmX, y + 18, 92, 120), _skinShadeBrush!);
		RenderTarget.FillRectangle(new Rect(palmX + (mirrored ? -8 : 8), y + 10, 92, 120), _skinBrush!);
		for (int i = 0; i < 4; i++)
		{
			float fingerY = y + i * 25;
			float fingerX = mirrored ? palmX - 74 : palmX + 82;
			RenderTarget.FillRoundedRectangle(new RoundedRectangle(new System.Drawing.RectangleF(fingerX, fingerY, 82, 20), 10, 10), _skinBrush!);
		}
		float thumbX = mirrored ? palmX - 42 : palmX + 75;
		RenderTarget.FillRoundedRectangle(new RoundedRectangle(new System.Drawing.RectangleF(thumbX, y + 96, 58, 24), 12, 12), _skinBrush!);
		RenderTarget.DrawRectangle(new Rect(palmX + (mirrored ? -8 : 8), y + 10, 92, 120), _skinShadeBrush!, 3.0f);
	}

	private void DrawPlayerFist(float centerX, float y, bool caught)
	{
		float width = caught ? 112.0f : 100.0f;
		RenderTarget.FillRoundedRectangle(new RoundedRectangle(new System.Drawing.RectangleF(centerX - width / 2, y, width, 92), 25, 25), _skinShadeBrush!);
		RenderTarget.FillRoundedRectangle(new RoundedRectangle(new System.Drawing.RectangleF(centerX - width / 2, y - 7, width, 88), 25, 25), _skinBrush!);
		for (int i = 0; i < 4; i++)
		{
			float knuckleX = centerX - width / 2 + 8 + i * 23;
			RenderTarget.FillEllipse(new Ellipse(new Vector2(knuckleX + 12, y - 3), 14, 14), _skinBrush!);
		}
		RenderTarget.DrawLine(new Vector2(centerX - 34, y + 47), new Vector2(centerX + 34, y + 47), _skinShadeBrush!, 3.0f);
		RenderTarget.FillRectangle(new Rect(centerX - 31, y + 80, 62, 82), _skinBrush!);
	}

	private void RenderTimingBar()
	{
		RenderTarget.FillRectangle(new Rect(125, 510, 710, 100), _panelBrush!);
		RenderTarget.FillRectangle(new Rect(BarLeft, BarTop, BarWidth, BarHeight), _creamBrush!);
		RenderTarget.FillRectangle(new Rect(_clearStart, BarTop, ClearWidth, BarHeight), _greenBrush!);
		RenderTarget.DrawRectangle(new Rect(BarLeft, BarTop, BarWidth, BarHeight), _darkBrush!, 4.0f);
		RenderTarget.FillRectangle(new Rect(_markerPosition - 5, BarTop - 14, 10, BarHeight + 28), _state == GameState.Caught ? _redBrush! : _goldBrush!);
		_bodyFont?.DrawText("SPACE로 멈추기", new Rect(365, 578, 230, 28), new Color4(1, 1, 1, 1));
	}

	private void RenderGameOver()
	{
		RenderTarget.FillRectangle(new Rect(150, 125, 660, 390), _panelBrush!);
		_titleFont?.DrawText("게임 오버", new Rect(190, 155, 580, 95), new Color4(0.95f, 0.24f, 0.20f, 1.0f));
		_headingFont?.DrawText($"도달 스테이지  {_stage}", new Rect(290, 285, 380, 50), new Color4(1, 1, 1, 1));
		_headingFont?.DrawText($"최고 기록  {_bestStage}", new Rect(290, 340, 380, 50), new Color4(1.0f, 0.82f, 0.20f, 1.0f));
		_bodyFont?.DrawText("SPACE  다시 도전", new Rect(335, 430, 290, 38), new Color4(1, 1, 1, 1));
	}

	private float GetPunchProgress()
	{
		float t = Math.Clamp((float)_stateTime / 1.25f, 0.0f, 1.0f);
		return MathF.Sin(t * MathF.PI);
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

	private static G2Font CreateFont(float size) => new("Malgun Gothic", size, FontWeight.Bold, Vortice.DirectWrite.FontStyle.Normal, TextAlignment.Center, ParagraphAlignment.Center);

	public override void Dispose()
	{
		_skinShadeBrush?.Dispose(); _skinBrush?.Dispose(); _redBrush?.Dispose(); _greenBrush?.Dispose();
		_goldBrush?.Dispose(); _creamBrush?.Dispose(); _panelBrush?.Dispose(); _darkBrush?.Dispose();
		_bodyFont?.Dispose(); _headingFont?.Dispose(); _chantFont?.Dispose(); _titleFont?.Dispose(); _background?.Dispose();
		base.Dispose();
	}
}
