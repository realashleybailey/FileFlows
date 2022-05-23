using FileFlows.Plugin;
using FileFlows.Server.Database.Managers;
using MySqlConnector;

namespace FileFlows.Server.Helpers;

using System;
using System.Collections.Generic;
using NPoco;
using FileFlows.Server.Models;
using System.Data.SQLite;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Text;

/// <summary>
/// Database helper for communicating with the database
/// </summary>
public class DbHelper
{
    private static Server.Database.Managers.DbManager Manager;
    /// <summary>
    /// Initializes the DbHelper with an instance of a DbManager
    /// </summary>
    internal static Task<bool> Initialize()
    {
        Logger.Instance.ILog("Initializing Database: " + AppSettings.Instance.DatabaseConnection);
        Manager = DbManager.GetManager(AppSettings.Instance.DatabaseConnection);
        return Manager.CreateDb();
    }

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// </summary>
    internal static bool UseMemoryCache => Manager.UseMemoryCache; 


    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public static Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new() => Manager.Select<T>();

    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public static Task<IEnumerable<T>> Select<T>(string where, params object[] arguments)
        where T : FileFlowObject, new()
        => Manager.Select<T>(where, arguments);

    /// <summary>
    /// Get names of types
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public static Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args) =>
        Manager.GetNames<T>(andWhere, args);

    /// <summary>
    /// Get names of types indexed by their UID
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public static Task<Dictionary<Guid, string>> GetIndexedNames<T>(string andWhere = "", params object[] args) =>
        Manager.GetIndexedNames<T>(andWhere, args);
    
    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public static Task<bool> NameInUse<T>(Guid uid, string name) =>
        Task.FromResult(Manager.NameInUse<T>(uid, name));

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> Single<T>() where T : FileFlowObject, new()
        => Manager.Single<T>();

    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
        => Manager.Single<T>(uid);

    /// <summary>
    /// Selects a single instance by its name
    /// </summary>
    /// <param name="name">the name of the item to select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a single instance</returns>
    public static Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
        => Manager.SingleByName<T>(name);

    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal static Task UpdateLastModified(Guid uid)
        => Manager.UpdateLastModified(uid);

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal static Task AddMany(FileFlowObject[] items) => Manager.AddMany(items);

    /// <summary>
    /// Selects a single item from the database
    /// </summary>
    /// <param name="andWhere">the and where clause</param>
    /// <param name="args">any parameters to the select statement</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>an single instance</returns>
    public static Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
        => Manager.Single<T>(andWhere, args);


    /// <summary>
    /// Updates an object
    /// </summary>
    /// <param name="obj">the object to update</param>
    /// <typeparam name="T">the object type</typeparam>
    /// <returns>the updated object</returns>
    public static Task<T> Update<T>(T obj) where T : FileFlowObject, new()
        => Manager.Update<T>(obj);


    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <typeparam name="T">The type of objects being deleted</typeparam>
    public static Task Delete<T>(params Guid[] uids) where T : FileFlowObject
        => Manager.Delete<T>(uids);


    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public static Task Delete(params Guid[] uids) => Manager.Delete(uids);
    
    
    /// <summary>
    /// Finds an existing library file in the database
    /// </summary>
    /// <param name="fullPath">the full path of the library file</param>
    /// <returns>the result of the known file</returns>
    public static Task<LibraryFile> FindKnownLibraryFile(string fullPath) =>
        Manager.FindKnownLibraryFile(fullPath);

    /// <summary>
    /// Finds an existing library file in the database by a fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the file</param>
    /// <returns>the result of the known file</returns>
    public static Task<LibraryFile> FindKnownLibraryByFingerprint(string fingerprint) =>
        Manager.FindKnownLibraryByFingerprint(fingerprint);

    /// <summary>
    /// Gets the next library file to process
    /// </summary>
    /// <param name="node">the node executing this library file</param>
    /// <param name="workerUid">the UID of the worker</param>
    /// <returns>the next library file to process</returns>
    public static Task<LibraryFile> GetNextLibraryFile(ProcessingNode node, Guid workerUid) =>
        Manager.GetNextLibraryFile(node, workerUid);


#if (DEBUG)
    /// <summary>
    /// Clean the database and purge old data
    /// </summary>
    /// <returns>True if successful</returns>
    public Task<bool> CleanDatabase() => Manager.CleanDatabase();
#endif

}