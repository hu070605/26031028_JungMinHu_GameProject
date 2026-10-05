using System.Windows.Forms;
using Vortice.DirectWrite;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
	private enum GameState { Title, FirstBori, BoriPause, SecondBori, Timing, Success, GameOver }
	private enum ButtonAction { None, Start, Exit, Retry, ReturnToTitle, ResetBestStage }

	private const float BarLeft = 91.0f;
	private const float BarWidth = 778.0f;
	private const float OriginalClearZoneLeft = 500.0f;
	private const float ClearWidth = 156.0f;
	private const float ClearZoneTop = 507.0f;
	private const float ClearZoneHeight = 56.0f;
	private const float ClearZoneBlankSampleLeft = 250.0f;
	private const float ClearZoneEdgeMargin = 20.0f;
	private const float FirstBoriDuration = 0.58f;
	private const float BoriPauseDuration = 0.14f;
	private const float SecondBoriDuration = 0.58f;
	private const float SuccessDuration = 0.65f;
	private const float InitialMarkerSpeed = 250.0f;
	private const float MarkerSpeedIncrease = 55.0f;
	private const float SpeedTransitionPoint = 900.0f;
	private const int SpeedTransitionStage = 13;
	private const float PostTransitionSpeedIncrease = 20.0f;
	private const float StartButtonLeft = 330.0f;
	private const float StartButtonTop = 340.0f;
	private const float StartButtonWidth = 300.0f;
	private const float StartButtonHeight = 104.0f;
	private const float ExitButtonLeft = 370.0f;
	private const float ExitButtonTop = 455.0f;
	private const float ExitButtonWidth = 220.0f;
	private const float ExitButtonHeight = 58.0f;
	private const float RetryButtonLeft = 345.0f;
	private const float RetryButtonTop = 285.0f;
	private const float RetryButtonWidth = 270.0f;
	private const float RetryButtonHeight = 70.0f;
	private const float ReturnTitleButtonLeft = 720.0f;
	private const float ReturnTitleButtonTop = 547.0f;
	private const float ReturnTitleButtonWidth = 220.0f;
	private const float ReturnTitleButtonHeight = 73.0f;
	private const float ResetButtonLeft = 770.0f;
	private const float ResetButtonTop = 563.0f;
	private const float ResetButtonWidth = 170.0f;
	private const float ResetButtonHeight = 57.0f;
	private const float ButtonAnimationDuration = 0.18f;
	private const float ButtonMaximumScale = 1.10f;
	private const float ScreenTextureScale = 1.6f;

	private GameState _state = GameState.Title;
	private int _stage = 1;
	private int _bestStage;
	private double _stateTime;
	private float _markerPosition;
	private float _markerDirection = 1.0f;
	private float _clearZoneLeft = OriginalClearZoneLeft;
	private ButtonAction _animatingButton;
	private double _buttonAnimationTime;

	private G2Texture? _titleBackground;
	private G2Texture? _titleLogo;
	private G2Texture? _titleStartButton;
	private G2Texture? _titleExitButton;
	private G2Texture? _titleBestScore;
	private G2Texture? _titleResetButton;
	private G2Texture? _gameplayScene;
	private G2Texture? _gameplayChant;
	private G2Texture? _gameplaySuccessCallout;
	private G2Texture? _stageBoard;
	private G2Texture? _timingMarker;
	private G2Texture? _gameOverScene;
	private G2Texture? _gameOverReturnTitleButton;
	private G2Font? _numberFont;
	private G2Font? _smallNumberFont;
	private G2AudioMp3? _stageClearSound;
	private G2AudioMp3? _gameOverSound;
	private G2AudioMp3? _buttonClickSound;
	private G2AudioMp3? _titleBgm;
	private G2AudioMp3? _gameplayBgm;

	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	protected override void Initialize()
	{
		ClearColor = new Color4(0.07f, 0.04f, 0.02f, 1.0f);
		_bestStage = LoadBestStage();

		LoadTitleResources();
		LoadGameplayResources();
		LoadGameOverResources();

		_numberFont = CreateFont(30);
		_smallNumberFont = CreateFont(22);
		_stageClearSound = new G2AudioMp3("resource/sound/stage_clear.mp3");
		_gameOverSound = new G2AudioMp3("resource/sound/game_over.mp3");
		_buttonClickSound = new G2AudioMp3("resource/sound/button_click.mp3");
		_titleBgm = new G2AudioMp3("resource/sound/title_bgm.mp3");
		_gameplayBgm = new G2AudioMp3("resource/sound/gameplay_bgm.mp3");
		_titleBgm.Play(true);
	}

	private void LoadTitleResources()
	{
		_titleBackground = new G2Texture("resource/gametitle/title_bg.png");
		_titleLogo = new G2Texture("resource/gametitle/title_boribori.png");
		_titleStartButton = new G2Texture("resource/gametitle/title_game_start.png");
		_titleExitButton = new G2Texture("resource/gametitle/title_game_exit.png");
		_titleBestScore = new G2Texture("resource/gametitle/title_best_score_blank.png");
		_titleResetButton = new G2Texture("resource/gametitle/title_reset.png");
	}

	private void LoadGameplayResources()
	{
		_gameplayScene = new G2Texture("resource/gameplay/gameplay_scene.png");
		_gameplayChant = new G2Texture("resource/gameplay/gameplay_boribori.png");
		_gameplaySuccessCallout = new G2Texture("resource/gameplay/gameplay_ssal.png");
		_stageBoard = new G2Texture("resource/gameplay/gameplay_stage.png");
		_timingMarker = new G2Texture("resource/gameplay/gameplay_timing-_marker.png");
	}

	private void LoadGameOverResources()
	{
		_gameOverScene = new G2Texture("resource/gameover/gameover_scene.png");
		_gameOverReturnTitleButton = new G2Texture("resource/gameover/gameover_return_title.png");
	}

	protected override void Update()
	{
		_stateTime += DeltaTime;
		if (Input.IsKeyDown(Keys.Escape))
		{
			Close();
			return;
		}

		if (_animatingButton != ButtonAction.None)
		{
			UpdateButtonAnimation();
			return;
		}

		switch (_state)
		{
			case GameState.Title:
				if (Input.IsKeyDown(Keys.Space) || IsLeftButtonClickedInside(
					StartButtonLeft, StartButtonTop, StartButtonWidth, StartButtonHeight))
				{
					if (Input.IsKeyDown(Keys.Space))
					{
						StartGame();
					}
					else
					{
						BeginButtonAnimation(ButtonAction.Start);
					}
				}
				else if (IsLeftButtonClickedInside(
					ExitButtonLeft, ExitButtonTop, ExitButtonWidth, ExitButtonHeight))
				{
					BeginButtonAnimation(ButtonAction.Exit);
				}
				else if (IsLeftButtonClickedInside(
					ResetButtonLeft, ResetButtonTop, ResetButtonWidth, ResetButtonHeight))
				{
					BeginButtonAnimation(ButtonAction.ResetBestStage);
				}
				break;
			case GameState.FirstBori:
				if (_stateTime >= FirstBoriDuration)
				{
					ChangeState(GameState.BoriPause);
				}
				break;
			case GameState.BoriPause:
				if (_stateTime >= BoriPauseDuration)
				{
					ChangeState(GameState.SecondBori);
				}
				break;
			case GameState.SecondBori:
				if (_stateTime >= SecondBoriDuration)
				{
					BeginTiming();
				}
				break;
			case GameState.Timing:
				UpdateTiming();
				break;
			case GameState.Success:
				if (_stateTime >= SuccessDuration)
				{
					_stage++;
					RandomizeClearZone();
					ChangeState(GameState.FirstBori);
				}
				break;
			case GameState.GameOver:
				if (IsRetryKeyPressed() ||
					IsLeftButtonClickedInside(
						RetryButtonLeft, RetryButtonTop, RetryButtonWidth, RetryButtonHeight))
				{
					if (IsRetryKeyPressed())
					{
						StartGame();
					}
					else
					{
						BeginButtonAnimation(ButtonAction.Retry);
					}
				}
				else if (IsLeftButtonClickedInside(
					ReturnTitleButtonLeft, ReturnTitleButtonTop,
					ReturnTitleButtonWidth, ReturnTitleButtonHeight))
				{
					BeginButtonAnimation(ButtonAction.ReturnToTitle);
				}
				break;
		}
	}

	private bool IsRetryKeyPressed()
	{
		if (Input.IsKeyDown(Keys.Space))
		{
			return true;
		}

		G2InputContext.InputState altState = Input.KeyState(Keys.Menu);
		bool isAltPressed = altState is
			G2InputContext.InputState.Down or
			G2InputContext.InputState.Press;
		return Input.IsKeyDown(Keys.Enter) && !isAltPressed;
	}

	private void BeginButtonAnimation(ButtonAction button)
	{
		_buttonClickSound?.Play(false);
		_animatingButton = button;
		_buttonAnimationTime = 0.0;
	}

	private void UpdateButtonAnimation()
	{
		_buttonAnimationTime += DeltaTime;
		if (_buttonAnimationTime < ButtonAnimationDuration)
		{
			return;
		}

		ButtonAction completedButton = _animatingButton;
		_animatingButton = ButtonAction.None;
		_buttonAnimationTime = 0.0;

		switch (completedButton)
		{
			case ButtonAction.Start:
			case ButtonAction.Retry:
				StartGame();
				break;
			case ButtonAction.Exit:
				Close();
				break;
			case ButtonAction.ReturnToTitle:
				ReturnToTitle();
				break;
			case ButtonAction.ResetBestStage:
				ResetBestStage();
				break;
		}
	}

	private float GetButtonScale(ButtonAction button)
	{
		if (_animatingButton != button)
		{
			return 1.0f;
		}

		float progress = Math.Clamp(
			(float)(_buttonAnimationTime / ButtonAnimationDuration), 0.0f, 1.0f);
		return 1.0f + (ButtonMaximumScale - 1.0f) * MathF.Sin(progress * MathF.PI);
	}

	private bool IsLeftButtonClickedInside(float left, float top, float width, float height)
	{
		if (!Input.IsButtonDown(MouseButtons.Left))
		{
			return false;
		}

		System.Drawing.PointF mouse = Input.MousePosition;
		return mouse.X >= left &&
			mouse.X <= left + width &&
			mouse.Y >= top &&
			mouse.Y <= top + height;
	}

	private void StartGame()
	{
		_titleBgm?.Stop();
		_gameplayBgm?.Play(true);
		_gameOverSound?.Stop();
		_stage = 1;
		RandomizeClearZone();
		ChangeState(GameState.FirstBori);
	}

	private void ReturnToTitle()
	{
		_gameplayBgm?.Stop();
		_gameOverSound?.Stop();
		_titleBgm?.Play(true);
		ChangeState(GameState.Title);
	}

	private void ResetBestStage()
	{
		_bestStage = 0;
		SaveBestStage();
	}

	private void BeginTiming()
	{
		_markerPosition = BarLeft;
		_markerDirection = 1.0f;
		ChangeState(GameState.Timing);
	}

	private void RandomizeClearZone()
	{
		float minimumLeft = BarLeft + ClearZoneEdgeMargin;
		float maximumLeft = BarLeft + BarWidth - ClearWidth - ClearZoneEdgeMargin;
		float minimumDistance = ClearWidth * 0.5f;
		float leftRangeEnd = Math.Clamp(_clearZoneLeft - minimumDistance, minimumLeft, maximumLeft);
		float rightRangeStart = Math.Clamp(_clearZoneLeft + minimumDistance, minimumLeft, maximumLeft);
		float leftRangeWidth = leftRangeEnd - minimumLeft;
		float rightRangeWidth = maximumLeft - rightRangeStart;

		// 이전 위치 주변을 제외한 두 구간에서 길이에 비례해 무작위로 선택한다.
		float offset = Random.Shared.NextSingle() * (leftRangeWidth + rightRangeWidth);
		_clearZoneLeft = offset < leftRangeWidth
			? minimumLeft + offset
			: rightRangeStart + offset - leftRangeWidth;
	}

	private void UpdateTiming()
	{
		MoveTimingMarker();

		if (Input.IsKeyDown(Keys.Space))
		{
			JudgeTiming();
		}
	}

	private void MoveTimingMarker()
	{
		float speed = _stage <= SpeedTransitionStage
			? Math.Min(
				SpeedTransitionPoint,
				InitialMarkerSpeed + (_stage - 1) * MarkerSpeedIncrease)
			: SpeedTransitionPoint +
				(_stage - SpeedTransitionStage) * PostTransitionSpeedIncrease;

		// 왕복 이동을 한 주기로 계산해 여러 번 튕기는 프레임에도 이동 거리를 보존한다.
		double relativePosition = _markerPosition - BarLeft;
		double period = BarWidth * 2.0;
		double phase = _markerDirection > 0.0f
			? relativePosition
			: period - relativePosition;
		phase = (phase + speed * DeltaTime) % period;
		if (phase >= BarWidth)
		{
			_markerPosition = BarLeft + (float)(period - phase);
			_markerDirection = -1.0f;
		}
		else
		{
			_markerPosition = BarLeft + (float)phase;
			_markerDirection = 1.0f;
		}
	}

	private void JudgeTiming()
	{
		bool cleared = _markerPosition >= _clearZoneLeft &&
			_markerPosition <= _clearZoneLeft + ClearWidth;
		if (!cleared)
		{
			_gameplayBgm?.Stop();
			_stageClearSound?.Stop();
			_gameOverSound?.Play(false);
			ChangeState(GameState.GameOver);
			return;
		}

		if (_stage > _bestStage)
		{
			_bestStage = _stage;
			SaveBestStage();
		}
		_stageClearSound?.Play(false);
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
			DrawFullScreen(_titleBackground);
			RenderTitle();
			return;
		}

		if (_state == GameState.GameOver)
		{
			DrawFullScreen(_gameOverScene);
			RenderRetryButtonEffect();
			RenderReturnTitleButton();
			RenderGameOverScores();
			return;
		}

		DrawFullScreen(_gameplayScene);
		RenderClearZone();
		RenderStage();
		RenderChant();
		if (_state is GameState.Timing or GameState.Success)
		{
			RenderTimingMarker();
		}
	}

	private static void DrawFullScreen(G2Texture? texture)
	{
		texture?.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));
	}

	private void RenderTitle()
	{
		_titleLogo?.Draw(new Rect(165, 75, 630, 217), new Rect(0, 0, 2137, 736));
		DrawScaledButton(
			_titleStartButton,
			new Rect(StartButtonLeft, StartButtonTop, StartButtonWidth, StartButtonHeight),
			new Rect(0, 0, 2126, 740),
			GetButtonScale(ButtonAction.Start));
		DrawScaledButton(
			_titleExitButton,
			new Rect(ExitButtonLeft, ExitButtonTop, ExitButtonWidth, ExitButtonHeight),
			new Rect(0, 0, 1845, 490),
			GetButtonScale(ButtonAction.Exit));
		_titleBestScore?.Draw(new Rect(350, 540, 260, 90), new Rect(0, 0, 2129, 739));
		_smallNumberFont?.DrawText(_bestStage.ToString(), new Rect(474, 560, 36, 40), new Color4(1.0f, 0.72f, 0.06f, 1.0f));
		DrawScaledButton(
			_titleResetButton,
			new Rect(ResetButtonLeft, ResetButtonTop, ResetButtonWidth, ResetButtonHeight),
			new Rect(0, 0, 2172, 724),
			GetButtonScale(ButtonAction.ResetBestStage));
	}

	private static void DrawScaledButton(G2Texture? texture, Rect destination, Rect source, float scale)
	{
		float scaledWidth = destination.Width * scale;
		float scaledHeight = destination.Height * scale;
		texture?.Draw(
			new Rect(
				destination.X + (destination.Width - scaledWidth) * 0.5f,
				destination.Y + (destination.Height - scaledHeight) * 0.5f,
				scaledWidth,
				scaledHeight),
			source);
	}

	private void RenderRetryButtonEffect()
	{
		if (_animatingButton != ButtonAction.Retry)
		{
			return;
		}

		DrawScaledButton(
			_gameOverScene,
			new Rect(RetryButtonLeft, RetryButtonTop, RetryButtonWidth, RetryButtonHeight),
			new Rect(
				RetryButtonLeft * ScreenTextureScale,
				RetryButtonTop * ScreenTextureScale,
				RetryButtonWidth * ScreenTextureScale,
				RetryButtonHeight * ScreenTextureScale),
			GetButtonScale(ButtonAction.Retry));
	}

	private void RenderReturnTitleButton()
	{
		DrawScaledButton(
			_gameOverReturnTitleButton,
			new Rect(
				ReturnTitleButtonLeft, ReturnTitleButtonTop,
				ReturnTitleButtonWidth, ReturnTitleButtonHeight),
			new Rect(0, 0, 2172, 724),
			GetButtonScale(ButtonAction.ReturnToTitle));
	}

	private void RenderStage()
	{
		_stageBoard?.Draw(new Rect(350, 16, 260, 59), new Rect(0, 0, 1811, 409));
		_numberFont?.DrawText(_stage.ToString(), new Rect(511, 25, 48, 38), new Color4(1.0f, 0.93f, 0.72f, 1.0f));
	}

	private void RenderClearZone()
	{
		// 배경 이미지에 고정된 초록 구간을 타이밍 바의 빈 부분으로 덮는다.
		_gameplayScene?.Draw(
			new Rect(OriginalClearZoneLeft, ClearZoneTop, ClearWidth, ClearZoneHeight),
			new Rect(
				ClearZoneBlankSampleLeft * ScreenTextureScale,
				ClearZoneTop * ScreenTextureScale,
				ClearWidth * ScreenTextureScale,
				ClearZoneHeight * ScreenTextureScale));

		// 원본 초록 구간을 이번 스테이지의 무작위 위치에 다시 그린다.
		_gameplayScene?.Draw(
			new Rect(_clearZoneLeft, ClearZoneTop, ClearWidth, ClearZoneHeight),
			new Rect(
				OriginalClearZoneLeft * ScreenTextureScale,
				ClearZoneTop * ScreenTextureScale,
				ClearWidth * ScreenTextureScale,
				ClearZoneHeight * ScreenTextureScale));
	}

	private void RenderChant()
	{
		if (_state == GameState.Success)
		{
			RenderSuccessCallout();
			return;
		}

		bool showChant = _state is
			GameState.FirstBori or
			GameState.BoriPause or
			GameState.SecondBori or
			GameState.Timing;

		if (!showChant)
		{
			return;
		}

		float pulse = 1.0f + 0.025f * MathF.Sin((float)_stateTime * 9.0f);
		float width = 324.0f * pulse;
		float height = 127.0f * pulse;
		_gameplayChant?.Draw(
			new Rect(480.0f - width * 0.5f, 164.0f - height * 0.5f, width, height),
			new Rect(0, 0, 1673, 654));
	}

	private void RenderSuccessCallout()
	{
		float popPhase = Math.Min(1.0f, (float)(_stateTime / 0.22));
		float scale = 1.0f + 0.12f * MathF.Sin(popPhase * MathF.PI);
		float width = 285.0f * scale;
		float height = 124.0f * scale;
		_gameplaySuccessCallout?.Draw(
			new Rect(480.0f - width * 0.5f, 164.0f - height * 0.5f, width, height),
			new Rect(0, 0, 1900, 828));
	}

	private void RenderTimingMarker()
	{
		_timingMarker?.Draw(new Rect(_markerPosition - 9, 509, 18, 52), new Rect(0, 0, 276, 1286));
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
		try
		{
			if (!File.Exists(BestStagePath))
			{
				return 0;
			}

			string savedValue = File.ReadAllText(BestStagePath);
			return int.TryParse(savedValue, out int bestStage) ? bestStage : 0;
		}
		catch
		{
			return 0;
		}
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
		_gameplayBgm?.Dispose();
		_titleBgm?.Dispose();
		_buttonClickSound?.Dispose();
		_gameOverSound?.Dispose();
		_stageClearSound?.Dispose();
		_smallNumberFont?.Dispose();
		_numberFont?.Dispose();
		_gameOverReturnTitleButton?.Dispose();
		_gameOverScene?.Dispose();
		_timingMarker?.Dispose();
		_stageBoard?.Dispose();
		_gameplaySuccessCallout?.Dispose();
		_gameplayChant?.Dispose();
		_gameplayScene?.Dispose();
		_titleBestScore?.Dispose();
		_titleResetButton?.Dispose();
		_titleExitButton?.Dispose();
		_titleStartButton?.Dispose();
		_titleLogo?.Dispose();
		_titleBackground?.Dispose();
		base.Dispose();
	}
}
