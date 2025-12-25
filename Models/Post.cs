using System;
using System.Collections.Generic;

namespace MonitoringSystem.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Change single image to list for multiple images
        public List<PostImage> Images { get; set; } = new List<PostImage>();

        // NEW: Likes list
        public List<PostLike> Likes { get; set; } = new List<PostLike>();
    }

    public class PostImage
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int PostId { get; set; }
        public Post? Post { get; set; }
    }

    // NEW: PostLike model
    public class PostLike
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PostId { get; set; }
        public Post? Post { get; set; }
    }
}
