﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs;

public class BlogPostTagListModel : BaseModel
{
    public IList<BlogPostTagModel> Tags { get; set; } = new List<BlogPostTagModel>();

    public int GetFontSize(BlogPostTagModel blogPostTag)
    {
        var itemWeights = Tags.Select(tag => tag.BlogPostCount).Select(dummy => (double)dummy).ToList();
        var stdDev = StdDev(itemWeights, out var mean);

        return GetFontSize(blogPostTag.BlogPostCount, mean, stdDev);
    }

    protected int GetFontSize(double weight, double mean, double stdDev)
    {
        var factor = weight - mean;

        if (factor != 0 && stdDev != 0) factor /= stdDev;

        return factor > 2 ? 150 :
            factor > 1 ? 120 :
            factor > 0.5 ? 100 :
            factor > -0.5 ? 90 :
            factor > -1 ? 85 :
            factor > -2 ? 80 :
            75;
    }

    protected double Mean(IEnumerable<double> values)
    {
        double sum = 0;
        var count = 0;

        foreach (var d in values)
        {
            sum += d;
            count++;
        }

        return sum / count;
    }

    protected double StdDev(IEnumerable<double> values, out double mean)
    {
        mean = Mean(values);
        double sumOfDiffSquares = 0;
        var count = 0;

        foreach (var d in values)
        {
            var diff = d - mean;
            sumOfDiffSquares += diff * diff;
            count++;
        }

        return Math.Sqrt(sumOfDiffSquares / count);
    }
}