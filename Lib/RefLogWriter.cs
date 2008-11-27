﻿/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Util;

namespace Gitty.Lib
{
    [Complete]
    public class RefLogWriter
    {
        /**
         * Writes reflog entry for ref specified by refName
         * 
         * @param repo
         *            repository to use
         * @param oldCommit
         *            previous commit
         * @param commit
         *            new commit
         * @param message
         *            reflog message
         * @param refName
         *            full ref name         
         */
        public static void WriteReflog(Repository repo, ObjectId oldCommit, ObjectId commit, String message, String refName)
        {
            String entry = BuildReflogString(repo, oldCommit, commit, message);

            DirectoryInfo directory = repo.Directory;

            FileInfo reflogfile = PathUtil.CombineFilePath(directory, "logs/" + refName);
            DirectoryInfo reflogdir = reflogfile.Directory;
            if (!reflogdir.Exists)
            {
                try
                {
                    reflogdir.Create();
                }
                catch (Exception)
                {
                    throw new IOException("Cannot create directory " + reflogdir);
                }
            }
            StreamWriter writer = new StreamWriter(reflogfile.OpenWrite());
            writer.WriteLine(entry);
            writer.Close();
        }

        private static String BuildReflogString(Repository repo, ObjectId oldCommit, ObjectId commit, String message)
        {
            PersonIdent me = new PersonIdent(repo);
            String initial = "";
            if (oldCommit == null)
            {
                oldCommit = ObjectId.ZeroId;
                initial = " (initial)";
            }
            String s = oldCommit.ToString() + " " + commit.ToString() + " "
                    + me.ToExternalString() + "\t" + message + initial;
            return s;
        }

    }

}
