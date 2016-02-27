﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Nez.Systems;
using Nez.Textures;
using Microsoft.Xna.Framework.Graphics;


namespace Nez
{
	public class Scene
	{
		public enum SceneResolutionPolicy
		{
			/// <summary>
			/// Default. RenderTarget matches the sceen size
			/// </summary>
			None,
			/// <summary>
			/// The entire application is visible in the specified area without trying to preserve the original aspect ratio. 
			/// Distortion can occur, and the application may appear stretched or compressed.
			/// </summary>
			ExactFit,
			/// <summary>
			/// The entire application fills the specified area, without distortion but possibly with some cropping, 
			/// while maintaining the original aspect ratio of the application.
			/// </summary>
			NoBorder,
			/// <summary>
			/// Pixel perfect version of NoBorder. Scaling is limited to integer values.
			/// </summary>
			NoBorderPixelPerfect,
			/// <summary>
			/// The entire application is visible in the specified area without distortion while maintaining the original 
			/// aspect ratio of the application. Borders can appear on two sides of the application.
			/// </summary>
			ShowAll,
			/// <summary>
			/// Pixel perfect version of ShowAll. Scaling is limited to integer values.
			/// </summary>
			ShowAllPixelPerfect,
			/// <summary>
			/// The application takes the height of the design resolution size and modifies the width of the internal
			/// canvas so that it fits the aspect ratio of the device.
			/// no distortion will occur however you must make sure your application works on different
			/// aspect ratios
			/// </summary>
			FixedHeight,
			/// <summary>
			/// Pixel perfect version of FixedHeight. Scaling is limited to integer values.
			/// </summary>
			FixedHeightPixelPerfect,
			/// <summary>
			/// The application takes the width of the design resolution size and modifies the height of the internal
			/// canvas so that it fits the aspect ratio of the device.
			/// no distortion will occur however you must make sure your application works on different
			/// aspect ratios
			/// </summary>
			FixedWidth,
			/// <summary>
			/// Pixel perfect version of FixedWidth. Scaling is limited to integer values.
			/// </summary>
			FixedWidthPixelPerfect
		}


		/// <summary>
		/// default scene Camera
		/// </summary>
		public Camera camera;

		/// <summary>
		/// clear color that is used in preRender to clear the screen
		/// </summary>
		public Color clearColor = Color.CornflowerBlue;

		/// <summary>
		/// clear color for the final render of the RenderTarget to the framebuffer
		/// </summary>
		public Color letterboxColor = Color.Black;

		/// <summary>
		/// SamplerState used for the final draw of the RenderTarget to the framebuffer
		/// </summary>
		public SamplerState samplerState = Core.defaultSamplerState;

		/// <summary>
		/// Scene-specific ContentManager. Use it to load up any resources that are needed only by this scene. If you have global/multi-scene
		/// resources you can use Core.contentManager to load them since Nez will not ever unload them.
		/// </summary>
		public readonly NezContentManager contentManager;

		/// <summary>
		/// global toggle for PostProcessors
		/// </summary>
		public bool enablePostProcessing = true;

		/// <summary>
		/// The list of entities within this Scene
		/// </summary>
		public EntityList entities;

		/// <summary>
		/// Manages a list of all the RenderableComponents that are currently on scene Entitys
		/// </summary>
		public readonly RenderableComponentList renderableComponents;

		/// <summary>
		/// Stoes and manages all entity processors
		/// </summary>
		public readonly EntityProcessorList entityProcessors;

		/// <summary>
		/// gets the size of the sceneRenderTarget
		/// </summary>
		/// <value>The size of the scene render texture.</value>
		public Point sceneRenderTargetSize
		{
			get { return new Point( _sceneRenderTarget.Width, _sceneRenderTarget.Height ); }
		}

		/// <summary>
		/// if the ResolutionPolicy is pixel perfect this will be set to the scale calculated for it
		/// </summary>
		public int pixelPerfectScale = 1;

		/// <summary>
		/// the final render to the screen can be deferred to this delegate if set. This is really only useful for cases where the final render
		/// might need a full screen size effect even though a small back buffer is used.
		/// </summary>
		/// <value>The final render delegate.</value>
		public IFinalRenderDelegate finalRenderDelegate
		{
			set
			{
				if( _finalRenderDelegate != null )
					_finalRenderDelegate.unload();

				_finalRenderDelegate = value;
				_finalRenderDelegate.scene = this;
				_finalRenderDelegate.onAddedToScene();
			}
			get
			{
				return _finalRenderDelegate;
			}
		}
		IFinalRenderDelegate _finalRenderDelegate;

		/// <summary>
		/// default resolution size used for all scenes
		/// </summary>
		static Point defaultDesignResolutionSize;

		/// <summary>
		/// default resolution policy used for all scenes
		/// </summary>
		static SceneResolutionPolicy defaultSceneResolutionPolicy = SceneResolutionPolicy.None;

		/// <summary>
		/// resolution policy used by the scene
		/// </summary>
		SceneResolutionPolicy _resolutionPolicy;

		/// <summary>
		/// design resolution size used by the scene
		/// </summary>
		Point _designResolutionSize;

		/// <summary>
		/// this gets setup based on the resolution policy and is used for the final blit of the RenderTarget
		/// </summary>
		Rectangle _finalRenderDestinationRect;

		RenderTarget2D _sceneRenderTarget;
		RenderTarget2D _destinationRenderTarget;
		Action<Texture2D> _screenshotRequestCallback;

		List<Renderer> _renderers = new List<Renderer>();
		readonly List<PostProcessor> _postProcessors = new List<PostProcessor>();


		/// <summary>
		/// sets the default design size and resolution policy that new scenes will use
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		/// <param name="sceneResolutionPolicy">Scene resolution policy.</param>
		public static void setDefaultDesignResolution( int width, int height, SceneResolutionPolicy sceneResolutionPolicy )
		{
			defaultDesignResolutionSize = new Point( width, height );
			defaultSceneResolutionPolicy = sceneResolutionPolicy;
		}


		#region Scene creation helpers

		/// <summary>
		/// helper that creates a scene with the DefaultRenderer attached and ready for use
		/// </summary>
		/// <returns>The with default renderer.</returns>
		public static Scene createWithDefaultRenderer( Color? clearColor = null )
		{
			var scene = new Scene();

			if( clearColor.HasValue )
				scene.clearColor = clearColor.Value;
			scene.addRenderer( new DefaultRenderer() );
			return scene;
		}


		/// <summary>
		/// helper that creates a scene of type T with the DefaultRenderer attached and ready for use
		/// </summary>
		/// <returns>The with default renderer.</returns>
		public static T createWithDefaultRenderer<T>( Color? clearColor = null ) where T : Scene, new()
		{
			var scene = new T();

			if( clearColor.HasValue )
				scene.clearColor = clearColor.Value;
			scene.addRenderer( new DefaultRenderer() );
			return scene;
		}


		/// <summary>
		/// helper that creates a scene with no Renderer
		/// </summary>
		/// <returns>The with default renderer.</returns>
		public static Scene create( Color? clearColor = null )
		{
			var scene = new Scene();

			if( clearColor.HasValue )
				scene.clearColor = clearColor.Value;

			return scene;
		}


		/// <summary>
		/// helper that creates a scene of type T with no Renderer
		/// </summary>
		/// <returns>The with default renderer.</returns>
		public static T create<T>( Color? clearColor = null ) where T : Scene, new()
		{
			var scene = new T();

			if( clearColor.HasValue )
				scene.clearColor = clearColor.Value;

			return scene;
		}

		#endregion


		public Scene()
		{
			camera = new Camera();
			entities = new EntityList( this );
			renderableComponents = new RenderableComponentList();
			contentManager = new NezContentManager();

			if( Core.entitySystemsEnabled )
				entityProcessors = new EntityProcessorList();

			// setup our resolution policy. we'll commit it in begin
			_resolutionPolicy = defaultSceneResolutionPolicy;
			_designResolutionSize = defaultDesignResolutionSize;

			initialize();
		}


		#region Scene lifecycle

		/// <summary>
		/// override this in Scene subclasses and do your loading here. This is called from the contructor after the scene sets itself up but
		/// before begin is ever called.
		/// </summary>
		public virtual void initialize()
		{}


		/// <summary>
		/// override this in Scene subclasses and do any unloading necessary here
		/// </summary>
		public virtual void unload()
		{}


		internal void begin()
		{
			Assert.isFalse( _renderers.Count == 0, "Scene has begun with no renderer. At least one renderer must be present before beginning a scene." );
			Physics.reset();

			// prep our render textures and take care of centering the camera origin. we have to set the RenderTarget
			updateResolutionScaler();
			Core.graphicsDevice.SetRenderTarget( _sceneRenderTarget );
			camera.centerOrigin();

			if( entityProcessors != null )
				entityProcessors.begin();
			Core.emitter.addObserver( CoreEvents.GraphicsDeviceReset, onGraphicsDeviceReset );
		}


		internal void end()
		{			
			for( var i = 0; i < _renderers.Count; i++ )
				_renderers[i].unload();

			for( var i = 0; i < _postProcessors.Count; i++ )
				_postProcessors[i].unload();

			Core.emitter.removeObserver( CoreEvents.GraphicsDeviceReset, onGraphicsDeviceReset );
			entities.removeAllEntities();
			camera.unload();
			camera = null;
			contentManager.Dispose();
			_sceneRenderTarget.Dispose();
			Physics.clear();

			if( _destinationRenderTarget != null )
				_destinationRenderTarget.Dispose();

			if( entityProcessors != null )
				entityProcessors.end();

			unload();
		}


		internal void update()
		{
			// we set the RenderTarget here so that the Viewport will match the RenderTarget properly
			Core.graphicsDevice.SetRenderTarget( _sceneRenderTarget );

			// update our lists in case they have any changes
			entities.updateLists();
			renderableComponents.updateLists();

			if( entityProcessors != null )
				entityProcessors.update();

			for( var i = 0; i < entities.Count; i++ )
			{
				var entity = entities[i];
				if( entity.enabled && ( entity.updateInterval == 1 || Time.frameCount % entity.updateInterval == 0 ) )
					entity.update();
			}
		}


		internal void preRender()
		{
			// Renderers should always have those that require RenderTarget first. They clear themselves and set themselves as
			// the current RenderTarget when they render
			if( _renderers[0].renderTarget == null )
			{
				Core.graphicsDevice.SetRenderTarget( _sceneRenderTarget );
				Core.graphicsDevice.Clear( clearColor );
			}
		}


		internal void render()
		{
			var lastRendererHadRenderTarget = false;
			for( var i = 0; i < _renderers.Count; i++ )
			{
				// MonoGame follows the XNA bullshit implementation so it will clear the entire buffer if we change the render target even if null.
				// Because of that, we track when we are done with our RenderTargets and clear the scene at that time.
				if( lastRendererHadRenderTarget )
				{
					Core.graphicsDevice.SetRenderTarget( _sceneRenderTarget );
					Core.graphicsDevice.Clear( clearColor );

					// force a Camera matrix update to account for the new Viewport size
					if( _renderers[i].camera != null )
						_renderers[i].camera.forceMatrixUpdate();
					camera.forceMatrixUpdate();
				}

				_renderers[i].render( this );
				lastRendererHadRenderTarget = _renderers[i].renderTarget != null;
			}
		}


		/// <summary>
		/// any PostProcessors present get to do their processing then we do the final render of the RenderTarget to the screen
		/// </summary>
		/// <returns>The render.</returns>
		internal void postRender( RenderTarget2D finalRenderTarget = null )
		{
			var enabledCounter = 0;
			if( enablePostProcessing )
			{
				for( var i = 0; i < _postProcessors.Count; i++ )
				{
					if( _postProcessors[i].enabled )
					{
						var isEven = Mathf.isEven( enabledCounter );
						enabledCounter++;
						_postProcessors[i].process( isEven ? _sceneRenderTarget : _destinationRenderTarget, isEven ? _destinationRenderTarget : _sceneRenderTarget );
					}
				}
			}

			// if we have a screenshot request deal with it before the final render to the backbuffer
			if( _screenshotRequestCallback != null )
			{
				var tex = new Texture2D( Core.graphicsDevice, _sceneRenderTarget.Width, _sceneRenderTarget.Height );
				var data = new int[tex.Bounds.Width * tex.Bounds.Height];
				( Mathf.isEven( enabledCounter ) ? _sceneRenderTarget : _destinationRenderTarget ).GetData<int>( data );
				tex.SetData<int>( data );
				_screenshotRequestCallback( tex );

				_screenshotRequestCallback = null;
			}

			// render our final result to the backbuffer or let our delegate do so
			if( _finalRenderDelegate != null )
			{
				_finalRenderDelegate.handleFinalRender( letterboxColor, Mathf.isEven( enabledCounter ) ? _sceneRenderTarget : _destinationRenderTarget, _finalRenderDestinationRect, samplerState );
			}
			else
			{
				Core.graphicsDevice.SetRenderTarget( finalRenderTarget );
				Core.graphicsDevice.Clear( letterboxColor );
				Graphics.instance.spriteBatch.Begin( SpriteSortMode.Deferred, BlendState.Opaque, samplerState, null, null );
				Graphics.instance.spriteBatch.Draw( Mathf.isEven( enabledCounter ) ? _sceneRenderTarget : _destinationRenderTarget, _finalRenderDestinationRect, Color.White );
				Graphics.instance.spriteBatch.End();
			}
		}


		void onGraphicsDeviceReset()
		{
			updateResolutionScaler();
		}

		#endregion


		#region Resolution Policy

		/// <summary>
		/// sets the design size and resolution policy then updates the render textures
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		/// <param name="sceneResolutionPolicy">Scene resolution policy.</param>
		public void setDesignResolution( int width, int height, SceneResolutionPolicy sceneResolutionPolicy )
		{
			_designResolutionSize = new Point( width, height );
			_resolutionPolicy = sceneResolutionPolicy;
			updateResolutionScaler();
		}


		void updateResolutionScaler()
		{
			var designSize = _designResolutionSize;
			var screenSize = new Point( Screen.backBufferWidth, Screen.backBufferHeight );
			var screenAspectRatio = (float)screenSize.X / (float)screenSize.Y;

			var renderTargetWidth = screenSize.X;
			var renderTargetHeight = screenSize.Y;

			var resolutionScaleX = (float)screenSize.X / (float)designSize.X;
			var resolutionScaleY = (float)screenSize.Y / (float)designSize.Y;

			var rectCalculated = false;

			// calculate the scale used by the PixelPerfect variants
			pixelPerfectScale = 1;
			if( _resolutionPolicy != SceneResolutionPolicy.None )
			{
				if( (float)designSize.X / (float)designSize.Y > screenAspectRatio )
					pixelPerfectScale = screenSize.X / designSize.X;
				else
					pixelPerfectScale = screenSize.Y / designSize.Y;

				if( pixelPerfectScale == 0 )
					pixelPerfectScale = 1;
			}

			switch( _resolutionPolicy )
			{
				case SceneResolutionPolicy.None:
					_finalRenderDestinationRect.X = _finalRenderDestinationRect.Y = 0;
					_finalRenderDestinationRect.Width = screenSize.X;
					_finalRenderDestinationRect.Height = screenSize.Y;
					rectCalculated = true;
					break;
				case SceneResolutionPolicy.ExactFit:
					// exact design size render texture
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;
					break;
				case SceneResolutionPolicy.NoBorder:
					// exact design size render texture
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;

					resolutionScaleX = resolutionScaleY = Math.Max( resolutionScaleX, resolutionScaleY );
					break;
				case SceneResolutionPolicy.NoBorderPixelPerfect:
					// exact design size render texture
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;

					// we are going to do some cropping so we need to use floats for the scale then round up
					pixelPerfectScale = 1;
					if( (float)designSize.X / (float)designSize.Y < screenAspectRatio )
					{
						var floatScale = (float)screenSize.X / (float)designSize.X;
						pixelPerfectScale = Mathf.ceilToInt( floatScale );
					}
					else
					{
						var floatScale = (float)screenSize.Y / (float)designSize.Y;
						pixelPerfectScale = Mathf.ceilToInt( floatScale );
					}

					if( pixelPerfectScale == 0 )
						pixelPerfectScale = 1;

					_finalRenderDestinationRect.Width = Mathf.ceilToInt( designSize.X * pixelPerfectScale );
					_finalRenderDestinationRect.Height = Mathf.ceilToInt( designSize.Y * pixelPerfectScale );
					_finalRenderDestinationRect.X = ( screenSize.X - _finalRenderDestinationRect.Width ) / 2;
					_finalRenderDestinationRect.Y = ( screenSize.Y - _finalRenderDestinationRect.Height ) / 2;
					rectCalculated = true;

					break;
				case SceneResolutionPolicy.ShowAll:
					resolutionScaleX = resolutionScaleY = Math.Min( resolutionScaleX, resolutionScaleY );

					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;
					break;
				case SceneResolutionPolicy.ShowAllPixelPerfect:
					// exact design size render texture
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;

					_finalRenderDestinationRect.Width = Mathf.ceilToInt( designSize.X * pixelPerfectScale );
					_finalRenderDestinationRect.Height = Mathf.ceilToInt( designSize.Y * pixelPerfectScale );
					_finalRenderDestinationRect.X = ( screenSize.X - _finalRenderDestinationRect.Width ) / 2;
					_finalRenderDestinationRect.Y = ( screenSize.Y - _finalRenderDestinationRect.Height ) / 2;
					rectCalculated = true;

					break;
				case SceneResolutionPolicy.FixedHeight:
					resolutionScaleX = resolutionScaleY;
					designSize.X = Mathf.ceilToInt( screenSize.X / resolutionScaleX );

					// exact design size render texture for height but not width
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;
					break;
				case SceneResolutionPolicy.FixedHeightPixelPerfect:
					// start with exact design size render texture height. the width may change
					renderTargetHeight = designSize.Y;

					_finalRenderDestinationRect.Width = Mathf.ceilToInt( designSize.X * resolutionScaleX );
					_finalRenderDestinationRect.Height = Mathf.ceilToInt( designSize.Y * pixelPerfectScale );
					_finalRenderDestinationRect.X = ( screenSize.X - _finalRenderDestinationRect.Width ) / 2;
					_finalRenderDestinationRect.Y = ( screenSize.Y - _finalRenderDestinationRect.Height ) / 2;
					rectCalculated = true;

					renderTargetWidth = (int)( designSize.X * resolutionScaleX / pixelPerfectScale );
					break;
				case SceneResolutionPolicy.FixedWidth:
					resolutionScaleY = resolutionScaleX;
					designSize.Y = Mathf.ceilToInt( screenSize.Y / resolutionScaleY );

					// exact design size render texture for width but not height
					renderTargetWidth = designSize.X;
					renderTargetHeight = designSize.Y;
					break;
				case SceneResolutionPolicy.FixedWidthPixelPerfect:
					// start with exact design size render texture width. the height may change
					renderTargetWidth = designSize.X;

					_finalRenderDestinationRect.Width = Mathf.ceilToInt( designSize.X * pixelPerfectScale );
					_finalRenderDestinationRect.Height = Mathf.ceilToInt( designSize.Y * resolutionScaleY );
					_finalRenderDestinationRect.X = ( screenSize.X - _finalRenderDestinationRect.Width ) / 2;
					_finalRenderDestinationRect.Y = ( screenSize.Y - _finalRenderDestinationRect.Height ) / 2;
					rectCalculated = true;

					renderTargetHeight = (int)( designSize.Y * resolutionScaleY / pixelPerfectScale );

					break;
			}

			// if we didnt already calculate a rect (None and all pixel perfect variants calculate it themselves) calculate it now
			if( !rectCalculated )
			{
				// calculate the display rect of the RenderTarget
				var renderWidth = designSize.X * resolutionScaleX;
				var renderHeight = designSize.Y * resolutionScaleY;

				_finalRenderDestinationRect = RectangleExt.fromFloats( ( screenSize.X - renderWidth ) / 2, ( screenSize.Y - renderHeight ) / 2, renderWidth, renderHeight );
			}


			// set some values in the Input class to translate mouse position to our scaled resolution
			var scaleX = renderTargetWidth / (float)_finalRenderDestinationRect.Width;
			var scaleY = renderTargetHeight / (float)_finalRenderDestinationRect.Height;

			Input._resolutionScale = new Vector2( scaleX, scaleY );
			Input._resolutionOffset = _finalRenderDestinationRect.Location;

			// resize our RenderTargets
			if( _sceneRenderTarget != null )
				_sceneRenderTarget.Dispose();
			_sceneRenderTarget = RenderTarget.create( renderTargetWidth, renderTargetHeight );

			// only create the destinationRenderTarget if it already exists, which would indicate we have PostProcessors
			if( _destinationRenderTarget != null )
			{
				_destinationRenderTarget.Dispose();
				_destinationRenderTarget = RenderTarget.create( renderTargetWidth, renderTargetHeight );
			}

			// notify the PostProcessors, Renderers and FinalRenderDelegate of the change in render texture size
			for( var i = 0; i < _postProcessors.Count; i++ )
				_postProcessors[i].onSceneBackBufferSizeChanged( renderTargetWidth, renderTargetHeight );

			for( var i = 0; i < _renderers.Count; i++ )
				_renderers[i].onSceneBackBufferSizeChanged( renderTargetWidth, renderTargetHeight );

			if( _finalRenderDelegate != null )
				_finalRenderDelegate.onSceneBackBufferSizeChanged( renderTargetWidth, renderTargetHeight );
		}

		#endregion


		#region Utils

		/// <summary>
		/// after the next draw completes this will clone the backbuffer and call callback with the clone. Note that you must dispose of the 
		/// Texture2D when done with it!
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void requestScreenshot( Action<Texture2D> callback )
		{
			_screenshotRequestCallback = callback;
		}
	

		/// <summary>
		/// Returns whether the timeSinceSceneLoad has passed the given time interval since the last frame. Ex: given 2.0f, this will return true once every 2 seconds
		/// </summary>
		/// <param name="interval">The time interval to check for</param>
		/// <returns></returns>
		public bool onInterval( float interval )
		{
			return (int)( ( Time.timeSinceSceneLoad - Time.deltaTime ) / interval ) < (int)( Time.timeSinceSceneLoad / interval );
		}

		#endregion


		#region Renderer/PostProcessor Management

		/// <summary>
		/// adds a Renderer to the scene
		/// </summary>
		/// <returns>The renderer.</returns>
		/// <param name="renderer">Renderer.</param>
		public Renderer addRenderer( Renderer renderer )
		{
			_renderers.Add( renderer );
			_renderers.Sort();
			return renderer;
		}


		/// <summary>
		/// removes the Renderer from the scene
		/// </summary>
		/// <param name="renderer">Renderer.</param>
		public void removeRenderer( Renderer renderer )
		{
			_renderers.Remove( renderer );
		}


		/// <summary>
		/// adds a PostProcessor to the scene. Sets the scene field and calls PostProcessor.onAddedToScene so that PostProcessors can load
		/// resources using the scenes ContentManager.
		/// </summary>
		/// <param name="postProcessor">Post processor.</param>
		public T addPostProcessor<T>( T postProcessor ) where T : PostProcessor
		{
			_postProcessors.Add( postProcessor );
			_postProcessors.Sort();
			postProcessor.scene = this;
			postProcessor.onAddedToScene();

			// lazily create the 2nd RenderTarget for post processing only when a PostProcessor is added
			if( _destinationRenderTarget == null )
			{
				if( _sceneRenderTarget != null )
					_destinationRenderTarget = RenderTarget.create( _sceneRenderTarget.Width, _sceneRenderTarget.Height );
				else
					_destinationRenderTarget = RenderTarget.create();
			}

			return postProcessor;
		}


		/// <summary>
		/// removes a PostProcessor. Note that unload is not called when removing so if you no longer need the PostProcessor be sure to call
		/// unload to free resources.
		/// </summary>
		/// <param name="step">Step.</param>
		public void removePostProcessor( PostProcessor step )
		{
			_postProcessors.Remove( step );
		}

		#endregion


		#region Entity Management

		/// <summary>
		/// add the Entity to this Scene, and return it
		/// </summary>
		/// <typeparam name="T">entity type</typeparam>
		/// <returns></returns>
		public Entity createEntity( string name )
		{
			var entity = new Entity( name );
			return addEntity( entity );
		}


		/// <summary>
		/// add the Entity to this Scene at position, and return it
		/// </summary>
		/// <returns>The entity.</returns>
		/// <param name="name">Name.</param>
		/// <param name="position">Position.</param>
		public Entity createEntity( string name, Vector2 position )
		{
			var entity = new Entity( name );
			entity.transform.position = position;
			return addEntity( entity );
		}


		/// <summary>
		/// adds an Entity to the Scene's Entities list
		/// </summary>
		/// <param name="entity">The Entity to add</param>
		public Entity addEntity( Entity entity )
		{
			Assert.isFalse( entities.contains( entity ), "You are attempting to add the same entity to a scene twice: {0}", entity );
			entities.add( entity );

			for( var i = 0; i < entity.transform.childCount; i++ )
				addEntity( entity.transform.getChild( i ).entity );

			return entity;
		}


		/// <summary>
		/// adds an Entity to the Scene's Entities list
		/// </summary>
		/// <param name="entity">The Entity to add</param>
		public T addEntity<T>( T entity ) where T : Entity 
		{
			Assert.isFalse( entities.contains( entity ), "You are attempting to add the same entity to a scene twice: {0}", entity );
			entities.add( entity );
			return entity;
		}


		/// <summary>
		/// removes all entities from the scene
		/// </summary>
		public void destroyAllEntities()
		{
			for( var i = 0; i < entities.Count; i++ )
				entities[i].destroy();
		}


		/// <summary>
		/// searches for and returns the first Entity with name
		/// </summary>
		/// <returns>The entity.</returns>
		/// <param name="name">Name.</param>
		public Entity findEntity( string name )
		{
			return entities.findEntity( name );
		}


		/// <summary>
		/// returns all entities with the given tag
		/// </summary>
		/// <returns>The entities by tag.</returns>
		/// <param name="tag">Tag.</param>
		public List<Entity> findEntitiesByTag( int tag )
		{
			return entities.entitiesWithTag( tag );
		}


		/// <summary>
		/// returns all entities of Type T
		/// </summary>
		/// <returns>The of type.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public List<Entity> entitiesOfType<T>() where T : Entity
		{
			return entities.entitiesOfType<T>();
		}

		#endregion


		#region Entity System Processors

		/// <summary>
		/// adds an EntitySystem processor to the scene
		/// </summary>
		/// <returns>The processor.</returns>
		/// <param name="processor">Processor.</param>
		public EntitySystem addEntityProcessor( EntitySystem processor )
		{
			processor.scene = this;
			entityProcessors.add( processor );
			return processor;
		}


		/// <summary>
		/// Removes an EntitySystem processor from the scene
		/// </summary>
		/// <param name="processor">Processor.</param>
		public void removeEntityProcessor( EntitySystem processor )
		{
			entityProcessors.remove( processor );
		}


		/// <summary>
		/// Gets an EntitySystem processor
		/// </summary>
		/// <returns>The processor.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T getEntityProcessor<T>() where T : EntitySystem
		{
			return entityProcessors.getProcessor<T>();
		}

		#endregion
	
	}
}

