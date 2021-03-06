﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using System.Net;
using ClassicalSharp.Gui;
using ClassicalSharp.Network;
using ClassicalSharp.TexturePack;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp {
	
	/// <summary> Represents a connection to either a multiplayer server, or an internal single player server. </summary>
	public abstract class INetworkProcessor {
		
		public abstract bool IsSinglePlayer { get; }
		
		/// <summary> Opens a connection to the given IP address and port, and prepares the initial state of the client. </summary>
		public abstract void Connect( IPAddress address, int port );
		
		public abstract void SendChat( string text, bool partial );
		
		/// <summary> Informs the server of the client's current position and orientation. </summary>
		public abstract void SendPosition( Vector3 pos, float yaw, float pitch );
		
		/// <summary> Informs the server that using the given mouse button,
		/// the client clicked on a particular block or entity. </summary>
		public abstract void SendPlayerClick( MouseButton button, bool buttonDown, byte targetId, PickedPos pos );
		
		public abstract void Tick( double delta );
		
		public abstract void Dispose();
		
		public string ServerName;
		public string ServerMotd;
		
		/// <summary> Whether the network processor is currently connected to a server. </summary>
		public bool Disconnected;
		
		/// <summary> Whether the client should use extended player list management, with group names and ranks. </summary>
		public bool UsingExtPlayerList;
		
		/// <summary> Whether the server supports handling PlayerClick packets from the client. </summary>
		public bool UsingPlayerClick;
		
		/// <summary> Whether the server can handle partial message packets or not. </summary>
		public bool ServerSupportsPartialMessages;
		
		/// <summary> Whether the server supports receiving all code page 437 characters from this client. </summary>
		public bool ServerSupportsFullCP437;
		
		
		#region Texture pack / terrain.png

		protected Game game;
		
		protected void WarningScreenTick( WarningScreen screen ) {
			string identifier = (string)screen.Metadata;
			DownloadedItem item;
			if( !game.AsyncDownloader.TryGetItem( identifier, out item ) || item.Data == null ) return;
			
			long contentLength = (long)item.Data;
			if( contentLength <= 0 ) return;
			string url = identifier.Substring( 3 );
			
			float contentLengthMB = (contentLength / 1024f / 1024f );
			string address = url;
			if( url.StartsWith( "https://" ) ) address = url.Substring( 8 );
			if( url.StartsWith( "http://" ) ) address = url.Substring( 7 );
			screen.SetText( "Do you want to download the server's texture pack?",
			               "Texture pack url:", address,
			               "Download size: " + contentLengthMB.ToString( "F3" ) + " MB" );
		}
		
		protected internal void RetrieveTexturePack( string url ) {
			if( !game.AcceptedUrls.HasEntry( url ) && !game.DeniedUrls.HasEntry( url ) ) {
				game.AsyncDownloader.RetrieveContentLength( url, true, "CL_" + url );
				string address = url;
				if( url.StartsWith( "https://" ) ) address = url.Substring( 8 );
				if( url.StartsWith( "http://" ) ) address = url.Substring( 7 );
				
				game.ShowWarning( new WarningScreen(
					game, "CL_" + url, true, true, "Do you want to download the server's texture pack?",
					DownloadTexturePack, null, WarningScreenTick,
					"Texture pack url:", address,
					"Download size: Determining..." ) );
			} else {
				DownloadTexturePack( url );
			}
		}
		
		void DownloadTexturePack( WarningScreen screen ) {
			DownloadTexturePack( ((string)screen.Metadata).Substring( 3 ) );
		}
		
		void DownloadTexturePack( string url ) {
			if( game.DeniedUrls.HasEntry( url ) ) return;
			DateTime lastModified = TextureCache.GetLastModifiedFromCache( url );
			string etag = TextureCache.GetETagFromCache( url, game.ETags );

			if( url.Contains( ".zip" ) )
				game.AsyncDownloader.DownloadData( url, true, "texturePack", 
				                                  lastModified, etag );
			else
				game.AsyncDownloader.DownloadImage( url, true, "terrain", 
				                                   lastModified, etag );
		}
		
		protected void ExtractDefault() {
			TexturePackExtractor extractor = new TexturePackExtractor();
			extractor.Extract( game.DefaultTexturePack, game );
			game.World.TextureUrl = null;
		}
		
		protected void CheckAsyncResources() {
			DownloadedItem item;
			if( game.AsyncDownloader.TryGetItem( "terrain", out item ) ) {
				if( item.Data != null ) {
					Bitmap bmp = (Bitmap)item.Data;					
					game.World.TextureUrl = item.Url;					
					game.Events.RaiseTexturePackChanged();
					
					if( !FastBitmap.CheckFormat( bmp.PixelFormat ) ) {
						Utils.LogDebug( "Converting terrain atlas to 32bpp image" );
						game.Drawer2D.ConvertTo32Bpp( ref bmp );
					}
					if( !game.ChangeTerrainAtlas( bmp ) ) { bmp.Dispose(); return; }
					TextureCache.AddToCache( item.Url, bmp );
					TextureCache.AddETagToCache( item.Url, item.ETag, game.ETags );
				} else if( item.ResponseCode == HttpStatusCode.NotModified ) {
					Bitmap bmp = TextureCache.GetBitmapFromCache( item.Url );
					if( bmp == null ) {// Should never happen, but handle anyways.
						ExtractDefault();
					} else if( item.Url != game.World.TextureUrl ) {
						game.Events.RaiseTexturePackChanged();
						if( !game.ChangeTerrainAtlas( bmp ) ) { bmp.Dispose(); return; }
					}
					
					if( bmp != null ) game.World.TextureUrl = item.Url;
				} else {
					ExtractDefault();
				}
			}
			
			if( game.AsyncDownloader.TryGetItem( "texturePack", out item ) ) {
				if( item.Data != null ) {
					game.World.TextureUrl = item.Url;
					
					TexturePackExtractor extractor = new TexturePackExtractor();
					extractor.Extract( (byte[])item.Data, game );
					TextureCache.AddToCache( item.Url, (byte[])item.Data );
					TextureCache.AddETagToCache( item.Url, item.ETag, game.ETags );
				} else if( item.ResponseCode == HttpStatusCode.NotModified ) {
					byte[] data = TextureCache.GetDataFromCache( item.Url );
					if( data == null ) { // Should never happen, but handle anyways.
						ExtractDefault();
					} else if( item.Url != game.World.TextureUrl ) {
						TexturePackExtractor extractor = new TexturePackExtractor();
						extractor.Extract( data, game );
					}
					
					if( data != null ) game.World.TextureUrl = item.Url;
				} else {
					ExtractDefault();
				}
			}
		}
		#endregion
	}
}