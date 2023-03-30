namespace API.services
{
    // this class lets us return a single object when grabbing the documents needed to initialize the cache.
    public class MovieReview
    {
        public List<Movie> MovieDocuments { get; set; }
        public List<List<Review>> ReviewDocuments { get; set; } // F for the frontend guys
    }
}

// to see the full return json, run the "api/Movies/initialize-cache" route in swagger

// to map this object into React, do something like (actual variables not correct):

/*function Reviews({ reviews }) {
  return (
    <div>
      <h2>Movies</h2>
      <ul>
        {reviews.movieDocuments.map(movie => (
          <li key={movie.id}>{movie.title}</li>
        ))}
      </ul>

      <h2>Reviews</h2>
      <ul>
        {reviews.reviewDocuments.map((reviewList, index) => (
        <div key={index}>
          <h2>List {index + 1}</h2>
          <ul>
            {reviewList.map(review => (
              <li key={review.id}>
                <h3>{review.title}</h3>
                <p>{review.body}</p>
              </li>
            ))}
          </ul>
        </div>
      ))}
      </ul>
    </div>
  );
}*/
