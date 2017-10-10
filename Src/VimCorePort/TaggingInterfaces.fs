﻿#light

namespace Vim
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Text.Outlining
open Microsoft.VisualStudio.Text.Tagging;
open System.Collections.ObjectModel;
open Microsoft.VisualStudio.Utilities
open System
open System.Diagnostics
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Collections.Generic
open System.Threading

/// A tagger source for asynchronous taggers.  This interface is consumed from multiple threads
/// and each method which is called on the background thread is labelled as such
/// be called on any thread
type IAsyncTaggerSource<'TData, 'TTag when 'TTag :> ITag> =

    /// Delay in milliseconds which should occur between the call to GetTags and the kicking off
    /// of a background task
    abstract Delay : Nullable<int>

    /// The current Snapshot.  
    ///
    /// Called from the main thread only
    abstract TextSnapshot : ITextSnapshot

    /// The current ITextView if this tagger is attached to a ITextView.  This is an optional
    /// value
    ///
    /// Called from the main thread only
    abstract TextViewOptional : ITextView

    /// This method is called to gather data on the UI thread which will then be passed
    /// down to the background thread for processing
    ///
    /// Called from the main thread only
    abstract GetDataForSnapshot : snapshot : ITextSnapshot -> 'TData

    /// Return the applicable tags for the given SnapshotSpan instance.  This will be
    /// called on a background thread and should respect the provided CancellationToken
    ///
    /// Called from the background thread only
    [<UsedInBackgroundThread>]
    abstract GetTagsInBackground : data : 'TData -> span : SnapshotSpan -> cancellationToken : CancellationToken -> ReadOnlyCollection<ITagSpan<'TTag>>

    /// To prevent needless spawning of Task<T> values the async tagger has the option
    /// of providing prompt data.  This method should only be used when determination
    /// of the tokens requires no calculation.
    ///
    /// Called from the main thread only
    abstract TryGetTagsPrompt : span : SnapshotSpan * [<Out>] tags : byref<IEnumerable<ITagSpan<'TTag>>> -> bool

    /// <summary>
    /// Raised by the source when the underlying source has changed.  All previously
    /// provided data should be considered incorrect after this event
    /// </summary>
    [<CLIEvent>]
    abstract Changed : IDelegateEvent<System.EventHandler>

type IBasicTaggerSource<'TTag when 'TTag :> ITag> = 

    /// Get the tags for the given SnapshotSpan
    abstract GetTags : span : SnapshotSpan -> ReadOnlyCollection<ITagSpan<'TTag>>

    /// Raised when the source changes in some way
    [<CLIEvent>]
    abstract Changed : IDelegateEvent<System.EventHandler>

[<StructuralEquality>]
[<NoComparison>]
[<Struct>]
type OutliningRegion =
    {
        Tag : OutliningRegionTag
        Span : SnapshotSpan
        Cookie : int
    }

/// Allows callers to create outlining regions over arbitrary SnapshotSpan values
type IAdhocOutliner =

    /// Get the ITextBuffer associated with this instance
    abstract TextBuffer : ITextBuffer

    /// Get all of the regions in the given ITextSnapshot
    abstract GetOutliningRegions : span : SnapshotSpan -> ReadOnlyCollection<OutliningRegion>

    /// Create an outlining region over the given SnapshotSpan.  The int value returned is 
    /// a cookie for later deleting the region
    abstract CreateOutliningRegion : span : SnapshotSpan -> spanTrackingMode : SpanTrackingMode -> text : string -> hint : string -> OutliningRegion

    /// Delete the previously created outlining region with the given cookie
    abstract DeleteOutliningRegion : cookie : int -> bool

    /// Raised when any outlining regions change
    [<CLIEvent>]
    abstract Changed : IDelegateEvent<System.EventHandler>